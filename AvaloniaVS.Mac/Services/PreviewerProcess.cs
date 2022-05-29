using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

using Avalonia.Remote.Protocol;
using Avalonia.Remote.Protocol.Designer;
using Avalonia.Remote.Protocol.Input;
using Avalonia.Remote.Protocol.Viewport;

using AvaloniaVS.Mac.Utils;

using AppKit;
using Foundation;
using ObjCRuntime;
using WebKit;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Jpeg;
using CoreGraphics;

namespace AvaloniaVS.Mac.Services;


public class PreviewerProcess
{
    private string _assemblyPath;
    private string _executablePath;
    private double _scaling = (1 / NSScreen.MainScreen.BackingScaleFactor);
    private double _dpi = 218;


    private Process _process;
    private IAvaloniaRemoteTransportConnection _connection;
    private IDisposable _listener;

    private WKWebView _webView = new WKWebView(new CoreGraphics.CGRect(0,0, 200, 200), new WKWebViewConfiguration());
    private ExceptionDetails _error;


    private NSImage img;

    public NSImage PreviewImage => img;

    /// <summary>
    /// Gets the current preview as a <see cref="NSImage"/>.
    /// </summary>
    public WKWebView WebView => _webView;

    public event EventHandler<NSImageView> DidUpdateImage;

    /// <summary>
    /// Gets the current error state as returned from the previewer process.
    /// </summary>
    public ExceptionDetails Error
    {
        get => _error;
        private set
        {
            if (!Equals(_error, value))
            {
                _error = value;
                ErrorChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the previewer process is currently running.
    /// </summary>
    public bool IsRunning => _process != null && !_process.HasExited;

    /// <summary>
    /// Gets a value indicating whether the previewer process is ready to receive messages.
    /// </summary>
    public bool IsReady => IsRunning && _connection != null;

    /// <summary>
    /// Gets scaling for the preview.
    /// </summary>
    public double Scaling => _scaling;

    public double Dpi => _dpi;

    /// <summary>
    /// Raised when the <see cref="Error"/> state changes.
    /// </summary>
    public event EventHandler ErrorChanged;

    /// <summary>
    /// Raised when a new frame is available in <see cref="Bitmap"/>.
    /// </summary>
    public event EventHandler FrameReceived;

    /// <summary>
    /// Raised when the underlying system process exits.
    /// </summary>
    public event EventHandler ProcessExited;


    public PreviewerProcess()
    {
        img = new NSImage();
    }


    /// <summary>
    /// Starts the previewer process.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly containing the XAML.</param>
    /// <param name="executablePath">The path to the executable to use for the preview.</param>
    /// <param name="hostAppPath">The path to the host application.</param>
    /// <returns>A task tracking the startup operation.</returns>
    public async Task StartAsync(string assemblyPath, string executablePath, string hostAppPath)
    {
        MonoDevelop.Core.LoggingService.LogInfo("Started PreviewerProcess.StartAsync()");

        if (_listener != null)
        {
            throw new InvalidOperationException("Previewer process already started.");
        }

        if (string.IsNullOrWhiteSpace(assemblyPath))
        {
            throw new ArgumentException(
                "Assembly path may not be null or an empty string.",
                nameof(assemblyPath));
        }

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new ArgumentException(
                "Executable path may not be null or an empty string.",
                nameof(executablePath));
        }

        if (string.IsNullOrWhiteSpace(hostAppPath))
        {
            throw new ArgumentException(
                "Executable path may not be null or an empty string.",
                nameof(executablePath));
        }

        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException(
                $"Could not find '{assemblyPath}'. " +
                "Please build your project to enable previewing and intellisense.");
        }

        var exeName = Path.GetFileNameWithoutExtension(executablePath);
        var adjustedExeName = Path.Combine(Path.GetDirectoryName(executablePath), exeName);

        if (!File.Exists(adjustedExeName))
        {
            throw new FileNotFoundException(
                $"Could not find executable '{executablePath}'. " +
                "Please build your project to enable previewing and intellisense.");
        }

        if (!File.Exists(hostAppPath))
        {
            throw new FileNotFoundException(
                $"Could not find executable '{hostAppPath}'. " +
                "Please build your project to enable previewing and intellisense.");
        }

        _assemblyPath = assemblyPath;
        _executablePath = executablePath;
        Error = null;

        var port = FreeTcpPort();
        var tcs = new TaskCompletionSource<object>();

        _listener = new BsonTcpTransport().Listen(
            IPAddress.Loopback,
            port,
#pragma warning disable VSTHRD101
                async t =>
                {
                    try
                    {
                        await ConnectionInitializedAsync(t);
                        tcs.TrySetResult(null);
                    }
                    catch (Exception ex)
                    {
                        MonoDevelop.Core.LoggingService.LogError("Error initializing connection");
                        tcs.TrySetException(ex);
                    }
                });
#pragma warning restore VSTHRD101

        var executableDir = Path.GetDirectoryName(_executablePath);
        var extensionDir = Path.GetDirectoryName(GetType().Assembly.Location);
        var targetName = Path.GetFileNameWithoutExtension(_executablePath);


        var runtimeConfigPath = Path.Combine(executableDir, targetName + ".runtimeconfig.json");
        var depsPath = Path.Combine(executableDir, targetName + ".deps.json");

        EnsureExists(runtimeConfigPath);
        EnsureExists(depsPath);
        EnsureExists(depsPath);

        //Use this args for web (for the future)
        //var args = $@"exec --runtimeconfig ""{runtimeConfigPath}"" --depsfile ""{depsPath}"" ""{hostAppPath}"" --transport tcp-bson://127.0.0.1:{port}/ --method html --html-url http://127.0.0.1:5000 ""{_executablePath}""";

        var args = $@"exec --runtimeconfig ""{runtimeConfigPath}"" --depsfile ""{depsPath}"" ""{hostAppPath}"" --transport tcp-bson://127.0.0.1:{port}/ ""{_executablePath}""";

        var processInfo = new ProcessStartInfo
        {
            Arguments = args,
            CreateNoWindow = true,
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        MonoDevelop.Core.LoggingService.LogInfo($"Starting previewer process for '{_executablePath}'");
        Console.WriteLine($"> dotnet {args}");

        var process = _process = Process.Start(processInfo);
        process.EnableRaisingEvents = true;
        process.OutputDataReceived += OnProcessOutputReceived;
        process.ErrorDataReceived += OnProcessErrorReceived;
        process.Exited += Abort;
        process.Exited += OnProcessExited;
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        void Abort(object sender, EventArgs e)
        {
            MonoDevelop.Core.LoggingService.LogInfo("Process exited while waiting for connection to be initialized.");
            tcs.TrySetException(new ApplicationException($"The previewer process exited unexpectedly with code {process.ExitCode}."));
        }

        try
        {
            MonoDevelop.Core.LoggingService.LogInfo($"Started previewer process for '{_executablePath}'. Waiting for connection to be initialized.");
            DidStart?.Invoke(this, EventArgs.Empty);
            await tcs.Task;
        }
        finally
        {
            process.Exited -= Abort;
        }

        MonoDevelop.Core.LoggingService.LogVerbose("Finished PreviewerProcess.StartAsync()");
    }

    /// <summary>
    /// Stops the previewer process.
    /// </summary>
    public void Stop()
    {
        MonoDevelop.Core.LoggingService.LogVerbose("Started PreviewerProcess.Stop()");
        MonoDevelop.Core.LoggingService.LogInfo("Stopping previewer process");

        _listener?.Dispose();
        _listener = null;

        if (_connection != null)
        {
            _connection.OnMessage -= ConnectionMessageReceived;
            _connection.OnException -= ConnectionExceptionReceived;
            _connection.Dispose();
            _connection = null;
        }

        if (_process != null && !_process.HasExited)
        {
            MonoDevelop.Core.LoggingService.LogDebug("Killing previewer process");

            try
            {
                // Kill the process. Do not set _process to null here, wait for ProcessExited to be called.
                _process.Kill();
            }
            catch (InvalidOperationException ex)
            {
                MonoDevelop.Core.LoggingService.LogDebug($"Failed to kill previewer process");
            }
        }

        _executablePath = null;

        MonoDevelop.Core.LoggingService.LogVerbose("Finished PreviewerProcess.Stop()");
        DidStop?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sets the scaling for the preview.
    /// </summary>
    /// <param name="scaling">The scaling factor.</param>
    /// <returns>A task tracking the operation.</returns>
    public async Task SetScalingAsync(double scaling)
    {
        _scaling = scaling;

        if (IsReady)
        {
            await SendAsync(new ClientRenderInfoMessage
            {
                DpiX = Dpi * _scaling,
                DpiY = Dpi * _scaling,
            });
        }
    }

    /// <summary>
    /// Updates the XAML to be previewed.
    /// </summary>
    /// <param name="xaml">The XAML.</param>
    /// <returns>A task tracking the operation.</returns>
    public async Task UpdateXamlAsync(string xaml)
    {
        if (_process == null)
        {
            throw new InvalidOperationException("Process not started.");
        }

        if (_connection == null)
        {
            throw new InvalidOperationException("Process not finished initializing.");
        }

        await SendAsync(new UpdateXamlMessage
        {
            AssemblyPath = _assemblyPath,
            Xaml = xaml,
        });
    }

    /// <summary>
    /// Sends an input message to the process.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>A task tracking the operation.</returns>
    public async Task SendInputAsync(InputEventMessageBase message)
    {
        if (_process == null)
        {
            throw new InvalidOperationException("Process not started.");
        }

        if (_connection == null)
        {
            throw new InvalidOperationException("Process not finished initializing.");
        }

        await SendAsync(message);
    }


    /// <summary>
    /// Stops the process and disposes of all resources.
    /// </summary>
    public void Dispose() => Stop();


    public event EventHandler DidStart;
    public event EventHandler DidStop;

    private async Task ConnectionInitializedAsync(IAvaloniaRemoteTransportConnection connection)
    {
        MonoDevelop.Core.LoggingService.LogVerbose("Started PreviewerProcess.ConnectionInitializedAsync()");
        MonoDevelop.Core.LoggingService.LogInfo("Connection initialized");

        if (!IsRunning)
        {
            MonoDevelop.Core.LoggingService.LogVerbose("ConnectionInitializedAsync detected process has stopped: aborting");
            return;
        }

        _connection = connection;
        _connection.OnException += ConnectionExceptionReceived;
        _connection.OnMessage += ConnectionMessageReceived;

        await SendAsync(new ClientSupportedPixelFormatsMessage
        {
            Formats = new[]
            {
                PixelFormat.Rgba8888
            }
        });

     
        await SetScalingAsync(_scaling);

        MonoDevelop.Core.LoggingService.LogVerbose("Finished PreviewerProcess.ConnectionInitializedAsync()");
    }

    private async Task SendAsync(object message)
    {
        MonoDevelop.Core.LoggingService.LogDebug($"=> Sending {message}");
        await _connection.Send(message);
    }

    private nfloat ScaleFactor => 1; //NSScreen.MainScreen.UserSpaceScaleFactor;


    private async Task OnMessageAsync(object message)
    {
        MonoDevelop.Core.LoggingService.LogVerbose("Started PreviewerProcess.OnMessageAsync()");
        MonoDevelop.Core.LoggingService.LogDebug($"<= {message}");

        switch (message)
        {
            case FrameMessage frame:
                {
                    if (frame.Data[0] != 0 && frame.Data[1] != 0 && frame.Data[2] != 0 && frame.Data[3] != 0)
                    {
                        Console.WriteLine($"Frame Width:{frame.Width} | Height:{frame.Height} | Data:{frame.Data.Length}");

                        try
                        {
                            using (var image = Image.LoadPixelData<Rgba32>(frame.Data, frame.Width, frame.Height))
                            {
                                using (var stream = new MemoryStream())
                                {
                                    await image.SaveAsJpegAsync(stream);

                                    var bytes = stream.ToArray();
                                    var imageData = NSData.FromArray(bytes);

                                    var size = new CGSize(frame.Width, frame.Height);
                                    img = new NSImage(imageData);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }

                        FrameReceived?.Invoke(this, EventArgs.Empty);
                    }

                    await SendAsync(new FrameReceivedMessage
                    {
                        SequenceId = frame.SequenceId
                    });
                    break;
                }
            case UpdateXamlResultMessage update:
                {
                    var exception = update.Exception;

                    if (exception == null && !string.IsNullOrWhiteSpace(update.Error))
                    {
                        exception = new ExceptionDetails { Message = update.Error };
                    }

                    Error = exception;

                    if (exception != null)
                    {
                        var xamlEx = new Exception($"Line:{exception.LineNumber ?? 0}, Position:{exception.LinePosition ?? 0} | Msg: {exception.Message}");
                        MonoDevelop.Core.LoggingService.LogError("UpdateXamlResult error");
                    }

                    break;
                }
            case RequestViewportResizeMessage resize:
                {
                    _webView = new WKWebView(new CoreGraphics.CGRect(new CoreGraphics.CGPoint(0, 0), new CoreGraphics.CGSize(resize.Width, resize.Height)), new WKWebViewConfiguration());
                }
                break;
        }
        MonoDevelop.Core.LoggingService.LogVerbose("Finished PreviewerProcess.OnMessageAsync()");
    }

    private void ConnectionMessageReceived(IAvaloniaRemoteTransportConnection connection, object message)
    {
        MonoDevelop.Core.Runtime.RunInMainThreadAsync(async () =>
        {
            await OnMessageAsync(message);
        });
    }

    private void ConnectionExceptionReceived(IAvaloniaRemoteTransportConnection connection, Exception ex)
    {
        MonoDevelop.Core.LoggingService.LogError("Connection error");
    }

    private void OnProcessOutputReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.Data))
        {
            MonoDevelop.Core.LoggingService.LogDebug($"<= {e.Data}");
        }
    }

    private void OnProcessErrorReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.Data))
        {
            MonoDevelop.Core.LoggingService.LogError($"<= {e.Data}");
        }
    }

    private void OnProcessExited(object sender, EventArgs e)
    {
        MonoDevelop.Core.LoggingService.LogInfo("Process exited");
        Stop();
        ProcessExited?.Invoke(this, EventArgs.Empty);
    }

    private static void EnsureExists(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Could not find '{path}'.");
        }
    }

    private static int FreeTcpPort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }
}