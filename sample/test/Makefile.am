TARGETS = WidgetViewer.exe 

assemblies=$(top_builddir)/glib/glib-sharp.dll $(top_builddir)/pango/pango-sharp.dll $(top_builddir)/atk/atk-sharp.dll $(top_builddir)/gdk/gdk-sharp.dll $(top_builddir)/gtk/gtk-sharp.dll
references = $(addprefix -r:, $(assemblies))

noinst_SCRIPTS = $(TARGETS)
CLEANFILES = $(TARGETS)

EXTRA_DIST = $(sources) ChangeLog

sources =			\
	TestCheckButton.cs 	\
	TestColorSelection.cs 	\
	TestRadioButton.cs 	\
	TestRange.cs 		\
	TestStatusbar.cs 	\
	TestDialog.cs 		\
	TestFlipping.cs 	\
	TestSizeGroup.cs	\
	TestCombo.cs 		\
	TestComboBox.cs		\
	WidgetViewer.cs

build_sources = $(addprefix $(srcdir)/, $(sources))

WidgetViewer.exe: $(build_sources) $(assemblies)
	$(CSC) -out:WidgetViewer.exe $(references) $(build_sources)

