using System;
using AppKit;
using Foundation;
using CoreGraphics;
using ObjCRuntime;

namespace AvaloniaVS.Mac.Views;

public class PreviewerControl : NSView
{
	private bool didBuildUI;
	private NSScrollView scrollView;
	private BlueprintView blueprintView;
	private CenteredClipView clipView;

	private NSImage image;
	private NSImageView imageView;
	private CGRect imageRect;

	// Use to adjust the height of the Document View.  Use this instead of other methods when you want to resize the view.
	NSLayoutConstraint viewHeightConstraint;

	// Use to adjust the width of the Document View.  Use this instead of other methods when you want to resize the view.
	NSLayoutConstraint viewWidthConstraint;

	private nfloat zoomFactor = 1.0f;

	public PreviewerControl(NSImage initialImage)
    {
		image = initialImage;
    }

	public void UpdateImage(NSImage newImage)
    {
		image = newImage;
		imageView.Image = newImage;

		imageView.Frame = new CGRect(imageView.Frame.Location, newImage.Size);
		clipView.Frame = imageView.Frame;

		Console.WriteLine($"Updating Image - Dimensions:{newImage.Size}");
    }

	public override void DrawRect(CGRect dirtyRect)
	{
		base.DrawRect(dirtyRect);

		if (!didBuildUI)
			BuildUI();
	}

	private void BuildUI()
	{
		if (didBuildUI)
			return;

		//Add Grid Pattern 
		blueprintView = new BlueprintView();
		blueprintView.Frame = Frame;
		blueprintView.AutoresizingMask = NSViewResizingMask.HeightSizable | NSViewResizingMask.WidthSizable;
		AddSubview(blueprintView);


		imageRect = new CGRect(0, 0, image.Size.Width, image.Size.Height);

		//Add ImageView
		imageView = CreateImageView(image);


		clipView = new CenteredClipView();
		clipView.DrawsBackground = false;
		clipView.AutoresizingMask = NSViewResizingMask.HeightSizable | NSViewResizingMask.WidthSizable;
		clipView.Frame = imageRect;
		clipView.DocumentView = imageView;


		scrollView = new NSScrollView(Frame);
		scrollView.DrawsBackground = false;
		scrollView.AutoresizingMask = NSViewResizingMask.HeightSizable | NSViewResizingMask.WidthSizable;
		scrollView.HasVerticalScroller = true;
		scrollView.HasHorizontalScroller = true;
		scrollView.DocumentView = clipView;
		scrollView.BorderType = NSBorderType.GrooveBorder;
		scrollView.ScrollerStyle = NSScrollerStyle.Overlay;


		AddSubview(scrollView);
		didBuildUI = true;
	}

	public void SetZoomFactor(nfloat zoom)
	{
		if (imageView == null)
			return;

		zoomFactor = zoom;

		if (viewHeightConstraint == null || viewWidthConstraint == null)
			SetContraints();

		viewHeightConstraint.Constant = imageView.Image.Size.Height * zoomFactor;
		viewWidthConstraint.Constant = imageView.Image.Size.Width * zoomFactor;

		imageView.AddConstraint(viewHeightConstraint);
		imageView.AddConstraint(viewWidthConstraint);
	}

	public void ZoomIn()
	{
		if (zoomFactor + 0.1 > 4)
			zoomFactor = 4;
		else if (zoomFactor == 0.05f)
			zoomFactor = 0.1f;
		else
			zoomFactor += 0.1f;
	}

	public void ZoomOut()
	{
		if (zoomFactor - 0.1 < 0.05)
			zoomFactor = 0.05f;
		else
			zoomFactor -= 0.1f;
	}

	public void ResetZoom() => SetZoomFactor(1.0f);

	public void ZoomToFit()
	{
		if (imageView == null)
			return;

		var imSize = imageView.Image.Size;
		var clipSize = clipView.Bounds.Size;

		//We want a 20 pixel gutter.  To make the calculations easier, adjust the clipbounds down to account for the gutter.
		//Use 2 * the pixel gutter, since we are adjusting only the height and width (this accounts for the
		//left and right margin combined, and the top and bottom margin combined).
		nfloat imageMargin = 40f;

		clipSize.Width -= imageMargin;
		clipSize.Height -= imageMargin;

		if (imSize.Width <= 0 && imSize.Height <= 0 && clipSize.Width <= 0 && clipSize.Height <= 0)
			return;

		var clipAspectRatio = clipSize.Width / clipSize.Height;
		var imAspectRatio = imSize.Width / imSize.Height;


		if (clipAspectRatio > imAspectRatio)
			zoomFactor = clipSize.Height / imSize.Height;
		else
			zoomFactor = clipSize.Width / imSize.Width;
	}

	public override void BeginGestureWithEvent(NSEvent theEvent)
	{
		base.BeginGestureWithEvent(theEvent);
	}

	public override void EndGestureWithEvent(NSEvent theEvent)
	{
		base.EndGestureWithEvent(theEvent);
	}

	public override void MagnifyWithEvent(NSEvent theEvent)
	{
		SetZoomFactor(theEvent.Magnification);
	}

	private void SetContraints()
	{
		foreach (var c in imageView.Constraints)
		{
			if (c.FirstAttribute == NSLayoutAttribute.Height)
				viewHeightConstraint = c;
			else if (c.FirstAttribute == NSLayoutAttribute.Width)
				viewWidthConstraint = c;
		}
	}

	private NSImageView CreateImageView(NSImage initialImage)
	{
		var imageView = NSImageView.FromImage(initialImage);
        imageView.WantsLayer = true;
		imageView.Shadow = new NSShadow();
		imageView.Layer.ShadowColor = NSColor.ControlShadow.CGColor;
		imageView.Layer.ShadowOpacity = 1.0f;
		imageView.Layer.ShadowOffset = new CGSize(0, 0);
		imageView.Layer.ShadowRadius = 10;

		return imageView;
	}


}


public class CenteredClipView : NSClipView
{
	public override CGRect ConstrainBoundsRect(CGRect proposedBounds)
	{
		// Anything to process
		if (DocumentView == null)
			return base.ConstrainBoundsRect(proposedBounds);

		// Get new bounds and insets
		var newClipBoundsRect = base.ConstrainBoundsRect(proposedBounds);

		// Get the `contentInsets` scaled to the future bounds size.
		var insets = ConvertContentInsetsToProposedBoundsSize(newClipBoundsRect.Size);

		// Get the insets in terms of the view geometry edges, accounting for flippedness.
		var minYInset = IsFlipped ? insets.Top : insets.Bottom;
		var maxYInset = IsFlipped ? insets.Bottom : insets.Top;
		var minXInset = insets.Left;
		var maxXInset = insets.Right;


		/*
		 * Get and outset the `documentView`'s frame by the scaled contentInsets.
		 * The outset frame is used to align and constrain the `newClipBoundsRect`.
		 */

		var documentFrame = DocumentView.Frame;
		var outsetDocumentFrame = new CGRect(documentFrame.GetMinX() - minXInset,
											 documentFrame.GetMinY() - minYInset,
											 (documentFrame.Width + (minXInset + maxXInset)),
											 (documentFrame.Height + (minYInset + maxYInset)));

		if (newClipBoundsRect.Width > outsetDocumentFrame.Width)
		{
			//If the clip bounds width is larger than the document, center the bounds around the document.
			newClipBoundsRect.X = outsetDocumentFrame.GetMinX() - (newClipBoundsRect.Width - outsetDocumentFrame.Width) / 2.0f;
		}
		else if (newClipBoundsRect.Width < outsetDocumentFrame.Width)
		{
			//Otherwise, the document is wider than the clip rect. Make sure that the clip rect stays within the document frame.
			if (newClipBoundsRect.GetMaxX() > outsetDocumentFrame.GetMaxX())
			{
				// The clip rect is outside the maxX edge of the document, bring it in.
				newClipBoundsRect.X = outsetDocumentFrame.GetMaxX() - newClipBoundsRect.Width;
			}
			else if (newClipBoundsRect.GetMinX() < outsetDocumentFrame.GetMinX())
			{
				// The clip rect is outside the minX edge of the document, bring it in.
				newClipBoundsRect.X = outsetDocumentFrame.GetMinX();
			}
		}

		if (newClipBoundsRect.Height > outsetDocumentFrame.Height)
		{
			// If the clip bounds height is larger than the document, center the bounds around the document.
			newClipBoundsRect.Y = outsetDocumentFrame.GetMinY() - (newClipBoundsRect.Height - outsetDocumentFrame.Height) / 2.0f;
		}
		else if (newClipBoundsRect.Height < outsetDocumentFrame.Height)
		{
			// Otherwise, the document is taller than the clip rect. Make sure that the clip rect stays within the document frame.
			if (newClipBoundsRect.GetMaxY() > outsetDocumentFrame.GetMaxY())
			{
				// The clip rect is outside the maxY edge of the document, bring it in.
				newClipBoundsRect.Y = outsetDocumentFrame.GetMaxY() - newClipBoundsRect.Height;
			}
			else if (newClipBoundsRect.GetMinY() < outsetDocumentFrame.GetMinY())
			{
				// The clip rect is outside the minY edge of the document, bring it in.
				newClipBoundsRect.Y = outsetDocumentFrame.GetMinY();
			}
		}

		var result = BackingAlignedRect(newClipBoundsRect, NSAlignmentOptions.AllEdgesNearest);
		Console.WriteLine($"Result: {result}");
		return result;
	}


	// The `contentInsets` scaled to the scale factor of a new potential bounds rect.Used by `ConstrainBoundsRect(NSRect)`.

	private NSEdgeInsets ConvertContentInsetsToProposedBoundsSize(CGSize proposedBoundsSize)
	{
		// Base the scale factor on the width scale factor to the new proposedBounds.
		var fromBoundsToProposedBoundsFactor = (Bounds.Width > 0) ? (proposedBoundsSize.Width / Bounds.Width) : 1.0f;

		// Scale the set `contentInsets` by the width scale factor.
		var newContentInsets = ContentInsets;
		newContentInsets.Top *= fromBoundsToProposedBoundsFactor;
		newContentInsets.Left *= fromBoundsToProposedBoundsFactor;
		newContentInsets.Bottom *= fromBoundsToProposedBoundsFactor;
		newContentInsets.Right *= fromBoundsToProposedBoundsFactor;

		// Return new insets
		return newContentInsets;
	}
}

public class BlueprintView : NSView
{
	public BlueprintView()
	{
		this.AcceptsTouchEvents = false;
	}

	public override void DrawRect(CGRect dirtyRect)
	{
		var context = NSGraphicsContext.CurrentContext.GraphicsPort;

		context.SetFillColor(NSColor.ControlBackground.CGColor);
		context.FillRect(dirtyRect);


		var thickLineColor = NSColor.ControlAccentColor.ColorWithAlphaComponent(0.4f); // NSColor.FromSrgb(100 / 255.0f, 149 / 255.0f, 237 / 255.0f, 0.3f);
		var thinLineColor = NSColor.ControlAccentColor.ColorWithAlphaComponent(0.2f); //NSColor.FromSrgb(100 / 255.0f, 149 / 255.0f, 237 / 255.0f, 0.1f); ;

		for (int i = 1; i < this.Bounds.Size.Height / 10; i++)
		{
			if (i % 10 == 0)
			{
				thickLineColor.Set();
			}
			else
			{
				thinLineColor.Set();
			}
			var pointFrom = new CGPoint(0, (i * GridSize - 0.5f));
			var pointTo = new CGPoint(this.Bounds.Size.Width, (i * GridSize - 0.5f));

			NSBezierPath.StrokeLine(pointFrom, pointTo);
		}


		for (int i = 1; i < this.Bounds.Size.Width / 10; i++)
		{
			if (i % 10 == 0)
			{
				thickLineColor.Set();
			}
			else
			{
				thinLineColor.Set();
			}
			var pointFrom = new CGPoint((i * GridSize - 0.5f), 0);
			var pointTo = new CGPoint((i * GridSize - 0.5f), this.Bounds.Size.Height);

			NSBezierPath.StrokeLine(pointFrom, pointTo);
		}

	}

	public float GridSize
	{
		get
		{
			return _gridSize;
		}
		set
		{
			if ((value >= MIN_GRID_SIZE) && (value <= MAX_GRID_SIZE))
			{
				_gridSize = value;
				NeedsDisplay = true;
			}
		}
	}

	//Fields
	private float _gridSize = 15;
	private const float MAX_GRID_SIZE = 30;
	private const float MIN_GRID_SIZE = 10;

}

