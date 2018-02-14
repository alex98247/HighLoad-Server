using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

namespace Kontur.ImageTransformer
{
    internal class AsyncHttpServer : IDisposable
    {
        public AsyncHttpServer()
        {
            listener = new HttpListener();
        }
        
        public void Start(string prefix)
        {
            lock (listener)
            {
                if (!isRunning)
                {
                    listener.Prefixes.Clear();
                    listener.Prefixes.Add(prefix);
                    listener.Start();

                    listenerThread = new Thread(Listen)
                    {
                        IsBackground = true,
                        Priority = ThreadPriority.Highest
                    };
                    listenerThread.Start();
                    
                    isRunning = true;
                }
            }
        }

        public void Stop()
        {
            lock (listener)
            {
                if (!isRunning)
                    return;

                listener.Stop();

                listenerThread.Abort();
                listenerThread.Join();
                
                isRunning = false;
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            Stop();

            listener.Close();
        }
        
        private void Listen()
        {
            while (true)
            {
                try
                {
                    if (listener.IsListening)
                    {
                        var context = listener.GetContext();
                        Task.Run(() => HandleContextAsync(context));
                    }
                    else Thread.Sleep(0);
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception error)
                {
                    // TODO: log errors
                }
            }
        }

        private async Task HandleContextAsync(HttpListenerContext listenerContext)
        {
            // TODO: implement request handling

            listenerContext.Response.StatusCode = (int)HttpStatusCode.OK;
            using (var writer = new StreamWriter(listenerContext.Response.OutputStream))
            {
                if (listenerContext.Request.Url.Segments[1] == "process/" /*&& listenerContext.Request.HttpMethod == "POST"*/)
                {
                    string transform = listenerContext.Request.Url.Segments[2].Replace("/", "");
                    switch (transform)
                    {
                        case "rotate-cw":
                            using (Image image = Image.FromStream(listenerContext.Request.InputStream))
                            {
                                image.RotateFlip(RotateFlipType.Rotate90FlipY);
                                using (MemoryStream mStream = new MemoryStream())
                                {
                                    Bitmap b = new Bitmap(500,200);
                                    Graphics g = Graphics.FromImage(b);
                                    g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height), new Rectangle(0,0,50,20), GraphicsUnit.Pixel);
                                    //image.Save(mStream, ImageFormat.Png);
                                    //b.Save("55555.png", ImageFormat.Png);
                                    b.Save(mStream, ImageFormat.Png);
                                    mStream.WriteTo(listenerContext.Response.OutputStream);
                                }
                            }
                            break;
                        case "rotate-ccw":
                            break;
                        case "flip-v":
                            break;
                        case "flip-h":
                            break;
                        default:
                            listenerContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            break;
                    }
                }
            }
        }

        private readonly HttpListener listener;

        private Thread listenerThread;
        private bool disposed;
        private volatile bool isRunning;
    }
}