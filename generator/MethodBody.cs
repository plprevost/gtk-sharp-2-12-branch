// GtkSharp.Generation.MethodBody.cs - The MethodBody Generation Class.
//
// Author: Mike Kestner <mkestner@speakeasy.net>
//
// Copyright (c) 2001-2003 Mike Kestner
// Copyright (c) 2003-2004 Novell, Inc.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of version 2 of the GNU General Public
// License as published by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// General Public License for more details.
//
// You should have received a copy of the GNU General Public
// License along with this program; if not, write to the
// Free Software Foundation, Inc., 59 Temple Place - Suite 330,
// Boston, MA 02111-1307, USA.

using System;
using System.IO;

namespace GtkSharp.Generation {

	public class MethodBody  {
		
		Parameters parameters;
		bool is_callback;
		public MethodBody (Parameters parms) 
		{
			parameters = parms;
		}

		private string CastFromInt (string type)
		{
			return type != "int" ? "(" + type + ") " : "";
		}

		public string GetCallString (bool is_set)
		{
			if (parameters.Count == 0)
				return String.Empty;

			string[] result = new string [parameters.Count];
			for (int i = 0; i < parameters.Count; i++) {
				Parameter p = parameters [i];
				IGeneratable igen = p.Generatable;

				bool is_prop = is_set && i == 0;

				if (i > 0 && parameters [i - 1].IsString && p.IsLength && p.PassAs == String.Empty) {
					string string_name = (i == 1 && is_set) ? "value" : parameters [i - 1].Name;
					result[i] = igen.CallByName (CastFromInt (p.CSType) + "System.Text.Encoding.UTF8.GetByteCount (" +  string_name + ")");
					continue;
				}

				if (is_prop)
					p.CallName = "value";
				else
					p.CallName = p.Name;
				string call_parm = is_callback ? p.CallStringCallback : p.CallString;

				if (p.IsUserData && parameters.IsHidden (p) && !parameters.HideData &&
					   (i == 0 || parameters [i - 1].Scope != "notified")) {
					var param = parameters [i - 1];
					var cb = param.Generatable as CallbackGen;
					if (cb != null && cb.WithParamGCHandle)
						call_parm = "(IntPtr)gch";
					else
						call_parm = "IntPtr.Zero";
				}

				result [i] += call_parm;
			}

			return String.Join (", ", result);
		}

		public void Initialize (GenerationInfo gen_info, bool is_callback)
		{
			Initialize (gen_info, false, false, String.Empty, is_callback);
		}

		public void Initialize (GenerationInfo gen_info, bool is_get, bool is_set, string indent, bool is_callback)
		{
			if (parameters.Count == 0)
				return;

			this.is_callback = is_callback;
			StreamWriter sw = gen_info.Writer;
			if (gen_info.AssemblyName == "gtk-sharp")
				sw.WriteLine (indent + "\t\t\tGtk.Application.AssertMainThread();");

			for (int i = 0; i < parameters.Count; i++) {
				Parameter p = parameters [i];

				IGeneratable gen = p.Generatable;
				string name = p.Name;
				if (is_set)
					name = "value";

				p.CallName = name;
				var prepare = is_callback ? p.PrepareCallback : p.Prepare;
				foreach (string prep in prepare)
					sw.WriteLine (indent + "\t\t\t" + prep);

				if (gen is CallbackGen) {
					CallbackGen cbgen = gen as CallbackGen;
					string wrapper = cbgen.GenWrapper(gen_info);
					switch (p.Scope) {
					case "notified":
						if (!cbgen.GenerateStaticWrapper) {
							sw.WriteLine (indent + "\t\t\t{0} {1}_wrapper = new {0} ({1});", wrapper, name);
						}
						sw.WriteLine (indent + "\t\t\tIntPtr {0};", parameters [i + 1].Name);
						sw.WriteLine (indent + "\t\t\t{0} {1};", parameters [i + 2].CSType, parameters [i + 2].Name);
						sw.WriteLine (indent + "\t\t\tif ({0} == null) {{", name);
						sw.WriteLine (indent + "\t\t\t\t{0} = IntPtr.Zero;", parameters [i + 1].Name);
						sw.WriteLine (indent + "\t\t\t\t{0} = null;", parameters [i + 2].Name);
						sw.WriteLine (indent + "\t\t\t} else {");
						sw.WriteLine (indent + "\t\t\t\t{0} = (IntPtr) GCHandle.Alloc ({1});", parameters [i + 1].Name, cbgen.GenerateStaticWrapper ? name : name + "_wrapper");
						sw.WriteLine (indent + "\t\t\t\t{0} = GLib.DestroyHelper.NotifyHandler;", parameters [i + 2].Name, parameters [i + 2].CSType);
						sw.WriteLine (indent + "\t\t\t}");
						break;

					case "async":
						if (cbgen.GenerateStaticWrapper) {
							sw.WriteLine (indent + "\t\t\tIntPtr gch = IntPtr.Zero;");
							sw.WriteLine (indent + "\t\t\tif ({0} != null) {{", name);
							sw.WriteLine (indent + "\t\t\t\tgch = (IntPtr)GCHandle.Alloc ({0});", name);
							sw.WriteLine (indent + "\t\t\t}");
						} else {
							sw.WriteLine (indent + "\t\t\t{0} {1}_wrapper = new {0} ({1});", wrapper, name);
							if (cbgen.WithParamGCHandle)
								sw.Write (indent + "\t\t\tGCHandle gch = ");
							sw.WriteLine ("{0}_wrapper.PersistUntilCalled ();", name);
						}
						break;
					case "call":
					default:
						if (p.Scope == String.Empty)
							Console.WriteLine ("Defaulting " + gen.Name + " param to 'call' scope in method " + gen_info.CurrentMember);
						if (!cbgen.GenerateStaticWrapper) {
							sw.WriteLine (indent + "\t\t\t{0} {1}_wrapper = new {0} ({1});", wrapper, name);
							if (cbgen.WithParamGCHandle)
								sw.WriteLine (indent + "\t\t\tGCHandle gch = GCHandle.Alloc ({0}_wrapper);", name);
						} else
							sw.WriteLine (indent + "\t\t\tGCHandle gch = GCHandle.Alloc ({0});", name);
						break;
					}

				}
			}

			if (ThrowsException)
				sw.WriteLine (indent + "\t\t\tIntPtr error = IntPtr.Zero;");
		}

		public void InitAccessor (StreamWriter sw, Signature sig, string indent)
		{
			sw.WriteLine (indent + "\t\t\t" + sig.AccessorType + " " + sig.AccessorName + ";");
		}

		public void Finish (StreamWriter sw, string indent)
		{
			foreach (Parameter p in parameters) {
				var finish = is_callback ? p.FinishCallback : p.Finish;
				foreach (string s in finish)
					sw.WriteLine (indent + "\t\t\t" + s);

				var cbgen = p.Generatable as CallbackGen;
				if (cbgen != null && p.Scope != "notified" && p.Scope != "async" && cbgen.WithParamGCHandle) {
					sw.WriteLine (indent + "\t\t\tgch.Free();");
				}
			}
		}

		public void FinishAccessor (StreamWriter sw, Signature sig, string indent)
		{
			sw.WriteLine (indent + "\t\t\treturn " + sig.AccessorName + ";");
		}

		public void HandleException (StreamWriter sw, string indent)
		{
			if (!ThrowsException)
				return;
			sw.WriteLine (indent + "\t\t\tif (error != IntPtr.Zero) throw new GLib.GException (error);");
		}
		
		public bool ThrowsException {
			get {
				int idx = parameters.Count - 1;

				while (idx >= 0) {
					if (parameters [idx].IsUserData)
						idx--;
					else if (parameters [idx].CType == "GError**")
						return true;
					else
						break;
				}
				return false;
			}
		}
	}
}

