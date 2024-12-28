using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using System;
using SharpDX.Direct3D;

public class vatSysHook : IDisposable
{
    private SharpDX.Direct3D11.Device device;
    private SwapChain swapChain;
    private RenderTargetView renderTargetView;
    private IntPtr hwnd;
    private SharpDX.Direct3D11.DeviceContext context;

    private SharpDX.Direct2D1.Factory d2dFactory;
    private RenderTarget d2dRenderTarget;
    private TextFormat textFormat;
    private SolidColorBrush textBrush;

    public vatSysHook(IntPtr hwnd)
    {
        this.hwnd = hwnd;
        InitializeDirectX();
    }

    private void InitializeDirectX()
    {
        var swapChainDescription = new SwapChainDescription
        {
            ModeDescription = new ModeDescription(800, 600, new Rational(60, 1), Format.R8G8B8A8_UNorm),
            SampleDescription = new SampleDescription(1, 0),
            Usage = Usage.RenderTargetOutput,
            BufferCount = 1,
            OutputHandle = hwnd,
            SwapEffect = SwapEffect.Discard,
            IsWindowed = false,
            Flags = SwapChainFlags.None
        };

        SharpDX.Direct3D11.Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, swapChainDescription, out device, out swapChain);
        var backBuffer = swapChain.GetBackBuffer<Surface>(0);
        var resource = backBuffer.QueryInterface<SharpDX.Direct3D11.Resource>();
        renderTargetView = new RenderTargetView(device, resource);
        context = device.ImmediateContext;
        context.OutputMerger.SetRenderTargets(renderTargetView);

        d2dFactory = new SharpDX.Direct2D1.Factory();

        var renderTargetProperties = new RenderTargetProperties
        {
            Type = RenderTargetType.Default,
            Usage = RenderTargetUsage.None,
            PixelFormat = new SharpDX.Direct2D1.PixelFormat(Format.Unknown, SharpDX.Direct2D1.AlphaMode.Premultiplied)
        };

        d2dRenderTarget = new WindowRenderTarget(d2dFactory, renderTargetProperties, new HwndRenderTargetProperties
        {
            Hwnd = hwnd,
            PixelSize = new Size2(800, 600),
            PresentOptions = PresentOptions.None
        });

        textFormat = new TextFormat(new SharpDX.DirectWrite.Factory(), "Arial", 32)
        {
            TextAlignment = TextAlignment.Leading,
            ParagraphAlignment = ParagraphAlignment.Near
        };

        textBrush = new SolidColorBrush(d2dRenderTarget, new SharpDX.Mathematics.Interop.RawColor4(1, 0, 0, 1)); // Red text
    }

    public void Draw()
    {
        context.ClearRenderTargetView(renderTargetView, new SharpDX.Mathematics.Interop.RawColor4(255, 0, 0, 255));

        d2dRenderTarget.BeginDraw();
        d2dRenderTarget.DrawText("Hello from vatACARS!", textFormat, new SharpDX.Mathematics.Interop.RawRectangleF(300, 300, 400, 100), textBrush);
        d2dRenderTarget.EndDraw();

        swapChain.Present(1, PresentFlags.None);
    }

    public void Dispose()
    {
        textBrush.Dispose();
        textFormat.Dispose();
        d2dRenderTarget.Dispose();
        d2dFactory.Dispose();
        renderTargetView.Dispose();
        swapChain.Dispose();
        device.Dispose();
    }
}