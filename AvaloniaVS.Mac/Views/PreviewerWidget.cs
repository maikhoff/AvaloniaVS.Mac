using AppKit;
using CoreGraphics;
using Foundation;
using Xwt;

namespace AvaloniaVS.Mac.Views;

public class PreviewerWidget : Widget
{
    VBox mainBox;
    Widget previewerWidget;
    PreviewerControl previewerControl;

    public PreviewerWidget()
    {
        CreateUI();
    }

    private void CreateUI()
    {
        mainBox = new VBox();
        previewerControl = new PreviewerControl(new NSImage());

        previewerWidget = Toolkit.CurrentEngine.WrapWidget(previewerControl, NativeWidgetSizing.DefaultPreferredSize);
        mainBox.PackStart(previewerWidget, true);

        Content = mainBox;
    }


    public void SetImage(NSImage image)
    {
        if (image == null)
            return;

        previewerControl.UpdateImage(image);
    }
   
}

