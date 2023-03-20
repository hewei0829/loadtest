using Docker.DotNet;
using Docker.DotNet.Models;


public class Program
{
    public static async Task Main(string[] args)
    {
        long pos = 0;
        var path = "test.json";
        var inputStream = new FileStream( path, FileMode.Open, FileAccess.Read );
        const int bufferSize = 1 << 10;
        var buffer = new byte[bufferSize];
        System.IO.Stream outputStream = new System.IO.MemoryStream();

        while ( true ) {
            var bytesRead = await inputStream.ReadAsync( buffer, (int)pos, bufferSize );
            if ( bytesRead == 0 ) break;
            await outputStream.WriteAsync( buffer, (int)pos, bytesRead );
        }


        pos = outputStream.Length;


        DockerMethod();

      //   var counter = 0;
      //   var max = args.Length is not 0 ? Convert.ToInt32(args[0]) : -1;
      //   while (max is -1 || counter < max)
      //   {
      //       Console.WriteLine($"Counter: {++counter}");
      //       await Task.Delay(TimeSpan.FromMilliseconds(1_000));
      //   }
    }
    
     private static void DockerMethod()
        {
            DockerClient client = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine"))
         .CreateClient();
            var parameters = new ContainersListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                {
                    "status",
                    new Dictionary<string, bool>
                    {
                        { "running", true}
                    }
                }

            }
            };

            var test =  client.Containers.ListContainersAsync(parameters).Result;

            foreach (var res in test)
            {
                if (res.Image == "j_meter:latest")
                {
                    var containerExecCreateResponse = client.Exec.ExecCreateContainerAsync(res.ID,
                        new ContainerExecCreateParameters
                        {
                            AttachStdout = true,
                            AttachStderr = true,
                            AttachStdin = true,
                            Cmd = new[] { "jmeter" }
                        }).Result;

                    // When
                    using (var stream = client.Exec.StartAndAttachContainerExecAsync(containerExecCreateResponse.ID, false).Result)
                    {
                        var buffer = System.Text.Encoding.ASCII.GetBytes("\n");
                        stream.WriteAsync(buffer, 0, buffer.Length, default).Wait();
                        string result = System.Text.Encoding.UTF8.GetString(buffer);

                        stream.CopyOutputToAsync(
                        null,
                        Console.OpenStandardOutput(),
                        Console.OpenStandardError(),
                        CancellationToken.None).Wait();
                    }
                }
            }
        }

}