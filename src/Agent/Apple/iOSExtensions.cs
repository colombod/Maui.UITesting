﻿#if IOS || MACCATALYST
using CoreGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UIKit;

namespace Microsoft.Maui.Automation;

internal static class iOSExtensions
{
	static string[] possibleTextPropertyNames = new string[]
	{
		"Title", "Text",
	};

	internal static string GetText(this UIView view)
		=> view switch
		{
			IUITextInput ti => TextFromUIInput(ti),
			UIButton b => b.CurrentTitle,
			_ => TextViaReflection(view, possibleTextPropertyNames)
		};

	static string TextViaReflection(UIView view, string[] propertyNames)
	{
		foreach (var name in propertyNames)
		{
			var prop = view.GetType().GetProperty("Text", typeof(string));
			if (prop is null)
				continue;
			if (!prop.CanRead)
				continue;
			if (prop.PropertyType != typeof(string))
				continue;
			return prop.GetValue(view) as string ?? "";
		}
		return "";
	}

	static string TextFromUIInput(IUITextInput ti)
	{
		var start = ti.BeginningOfDocument;
		var end = ti.EndOfDocument;
		var range = ti.GetTextRange(start, end);
		return ti.TextInRange(range);
	}

	public static Element GetElement(this UIKit.UIView uiView, IApplication application, string parentId = "", int currentDepth = -1, int maxDepth = -1)
	{
		var scale = uiView.Window?.Screen?.NativeScale.Value ?? 1.0f;

		var viewFrame = uiView.Frame.ToFrame();
        var windowFrame = uiView.ConvertRectToView(uiView.Bounds, uiView.Window).ToFrame();

#if MACCATALYST
		var nsWindow = UINSWindow.From(uiView.Window);
		var screenFrame = nsWindow.ConvertRectToScreen(uiView.Frame).ToFrame();
#else
        var screenFrame = UIAccessibility.ConvertFrameToScreenCoordinates(uiView.Frame, uiView).ToFrame();
#endif

        var e = new Element(application, Platform.Ios, uiView.Handle.ToString(), uiView, parentId)
		{
			AutomationId = uiView.AccessibilityIdentifier ?? string.Empty,
			Visible = !uiView.Hidden,
			Enabled = uiView.UserInteractionEnabled,
			Focused = uiView.Focused,
			ViewFrame = viewFrame,
			WindowFrame = windowFrame,
			ScreenFrame = screenFrame,
			Density = scale,
			Text = uiView.GetText() ?? string.Empty
		};

		if (maxDepth <= 0 || (currentDepth + 1 <= maxDepth))
		{
			var children = uiView.Subviews?.Select(s => s.GetElement(application, e.Id, currentDepth + 1, maxDepth))
					?.ToList() ?? new List<Element>();

			e.Children.AddRange(children);
		}
		return e;
	}

	public static Frame ToFrame(this CGRect rect)
		=> new Frame
		{
			X = (int)rect.X,
			Y = (int)rect.Y,
			Width = (int)rect.Width,
			Height = (int)rect.Height,
		};

	public static Element GetElement(this UIWindow window, IApplication application, int currentDepth = -1, int maxDepth = -1)
	{
        var scale = window?.Screen?.NativeScale.Value ?? 1.0f;

#if MACCATALYST
        var nsWindow = UINSWindow.From(window);
		
		var viewFrame = window.Frame.ToFrame();
		var windowFrame = nsWindow.Frame.ToFrame();
		var screenFrame = nsWindow.ConvertRectToScreen(window.Frame).ToFrame();
#else
        var viewFrame = window.Frame.ToFrame();
        var windowFrame = window.Frame.ToFrame();
        var screenFrame = UIAccessibility.ConvertFrameToScreenCoordinates(window.Frame, window).ToFrame();
#endif
        var e = new Element(application, Platform.Ios, window.Handle.ToString(), window)
		{
			AutomationId = window.AccessibilityIdentifier ?? window.Handle.ToString(),
			ViewFrame = viewFrame,
			WindowFrame	= windowFrame,
			ScreenFrame = screenFrame,
			Density = scale,
			Text = string.Empty
		};

		if (maxDepth <= 0 || (currentDepth + 1 <= maxDepth))
		{
			var children = window.Subviews?.Select(s => s.GetElement(application, e.Id, currentDepth + 1, maxDepth))?.ToList() ?? new List<Element>();

			e.Children.AddRange(children);
		}
		return e;
	}
	public static Task<PerformActionResult> PerformAction(this UIKit.UIView view, string action, string elementId, params string[] arguments)
	{
		if (action == Actions.Tap)
		{
			if (view is UIControl ctrl)
			{
				ctrl.InvokeOnMainThread(() =>
					ctrl.SendActionForControlEvents(UIControlEvent.TouchUpInside));
				return Task.FromResult(PerformActionResult.Ok());
			}
		}
		else if (action == Actions.InputText)
		{
			if (view is IUITextInput inputView)
			{
				var text = arguments.FirstOrDefault();
				inputView.InsertText(text);
			}
		}

		throw new NotSupportedException($"PerformAction {action} is not supported.");
	}
}
#endif
