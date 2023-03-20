using Microsoft.AspNetCore.Mvc;
using System.IO;
using StackExchange.Redis;
using Docker.DotNet;
using Docker.DotNet.Models;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Net.Http.Headers;
using System.Xml;


namespace webapi.Controllers;
using Microsoft.Extensions.Caching.Memory;

[ApiController]
[Route("api/test")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _hostingEnvironment;

    private static IMemoryCache _memoryCache;



    public WeatherForecastController(IMemoryCache cache, ILogger<WeatherForecastController> logger,Microsoft.AspNetCore.Hosting.IWebHostEnvironment  hostingEnvironment)
    {
        _logger = logger;
        _hostingEnvironment = hostingEnvironment;
        _memoryCache = cache;
    }

    
    [Route("{fileName}")]
    [HttpPost]
    public async Task<IActionResult> AddTestFile(string fileName)
     {
        using (var reader = new StreamReader(Request.Body))
            {
                var body = await reader.ReadToEndAsync();
                System.IO.File.WriteAllText("/home/node/app/" + fileName, body);
            }                  
                return Ok();
     }


    private DateTime ToEpochTime(long unixTimeStamp)
    {
    // Unix timestamp is seconds past epoch
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddMilliseconds( unixTimeStamp );
        return dateTime;
    }

    private DateTime ToLastestRoundTime(long unixTimeStamp, long sampleRate)
    {
        var rountTick = (unixTimeStamp/(sampleRate *1000) )*(sampleRate *1000);
    // Unix timestamp is seconds past epoch
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddMilliseconds( rountTick );
        return dateTime;
    }

    [Route("AggregateResultMock")]
    [HttpGet]
    public async Task<AggregatedResults> Mock()
     {
               var rep = new AggregatedResults();
         rep.listData = new List<AggregatedResult>();
         var baseTime = DateTime.Now.AddDays(-1);
         var rnd = new Random();
         var length = rnd.Next(12,50);
         for(int i = 0; i < length; i ++)
         {
            rep.listData.Add(new AggregatedResult(baseTime, 100 + i *30));
            baseTime = baseTime.AddMinutes(1);
         }

         return rep;
     }

    [Route("testRunResult/{instanceName}")]
    [HttpGet]
    public async Task<AggregatedResults> GetTestResult(string instanceName)
    {
        //TODO: Think about what should be round
        var sampleRate = ParseXMLAndGetDuration(instanceName)/20;
        return  await RunningInstanceResult(instanceName,sampleRate);
    }

    ///this is try to parse the xml and get the run duration
    private int ParseXMLAndGetDuration(string instanceName)
    {
        // var template =  instanceName.Split("-")[0];
        // var path = "/home/node/app/" + template;
        // XmlDocument xmlDoc= new XmlDocument();
        // xmlDoc.Load(path);
        return 180;
    }


    [Route("Aggregate/{fileName}/{sampleRate}")]
    [HttpGet]
    public async Task<AggregatedResults> GetAggrecatedResult(string fileName, int sampleRate)
    {
       return  await RunningInstanceResult(fileName,sampleRate);
    }

    private async Task<AggregatedResults> RunningInstanceResult(string fileName, int sampleRate)
     {
         var rep = new AggregatedResults();
         rep.listData = new List<AggregatedResult>();

        this.Response.StatusCode = 200;
        var path = "/home/node/app/" + fileName;
         //var path = _hostingEnvironment.ContentRootPath + "/"  + fileName;

        // this.Response.Headers.Add( HeaderNames.ContentDisposition, $"attachment; filename=\"{path}\"" );
        // this.Response.Headers.Add( HeaderNames.ContentType, "application/octet-stream"  );
        var inputStream = new FileStream( path, FileMode.Open, FileAccess.Read );
        var outputStream = this.Response.Body;
        const int bufferSize = 1 << 12;
        var buffer = new byte[bufferSize];

        using (var fileStream =  new FileStream( path, FileMode.Open, FileAccess.Read ))
        using (var streamReader = new StreamReader(fileStream, System.Text.Encoding.UTF8, true, bufferSize)) {
            String line;
            var count = 0;
            var first = true;
            var second = true;
            DateTime baseTime = DateTime.MinValue;
            DateTime nextTime = DateTime.MinValue;

            while ((line = await streamReader.ReadLineAsync()) != null)
            {
          
                if(first) 
                {
                    first = false;
                    continue;
                } 
                else if(second)
                {
                    second = false;
                    var data = line.Split(",");
                    var time = ToLastestRoundTime(  long.Parse(data[0]), (long) sampleRate );    
                    baseTime = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0);
                    nextTime = baseTime.AddSeconds(sampleRate);              
                }
                else
                {
                    var data = line.Split(",");
                    var time = ToEpochTime(  long.Parse(data[0]) );    

                    if(time >= nextTime)
                    {
                        if(count != 0)
                        {
                            rep.listData.Add(new AggregatedResult(baseTime,count));
                        }
                        baseTime = nextTime;
                        nextTime = baseTime.AddSeconds(sampleRate);
                        count = 0;
                    }
                    else
                    {
                        count ++;
                    }
                }


                // var data = line.Split(",");
                // var time = ToEpochTime(  long.Parse(data[0]) );            
            // Process line
            }
             rep.listData.Add(new AggregatedResult(baseTime,count));
        }

        return rep;

     }


    // [Route("{fileName}")]
    // [HttpPost]
    // public async Task<IActionResult> AddTestFile(string fileName)
    //  {
    //     using (var reader = new StreamReader(Request.Body))
    //         {
    //             var body = await reader.ReadToEndAsync();
    //             System.IO.File.WriteAllText("/home/node/app/" + fileName, body);
    //         }                  
    //             return Ok();
    //  }


    [Route("Stream/{fileName}/{section}")]
    [HttpGet]
    public async Task GetStatus(string fileName, string section)
     {
         long pos = 0;
         if (!_memoryCache.TryGetValue(section, out pos))
         {
             _memoryCache.Set(section,pos);
         }
        

        this.Response.StatusCode = 200;
         var path = "/home/node/app/" + fileName;

        this.Response.Headers.Add( HeaderNames.ContentDisposition, $"attachment; filename=\"{path}\"" );
        this.Response.Headers.Add( HeaderNames.ContentType, "application/octet-stream"  );
        var inputStream = new FileStream( path, FileMode.Open, FileAccess.Read );
        var outputStream = this.Response.Body;
        const int bufferSize = 1 << 12;
        var buffer = new byte[bufferSize];

        try
        {
            var streamLength = 0;

            inputStream.Position = pos;

            while ( true ) {
                var bytesRead = await inputStream.ReadAsync( buffer, 0, bufferSize );
                if ( bytesRead == 0 ) break;
                streamLength += bytesRead;
                await outputStream.WriteAsync( buffer, 0, bytesRead );
            }
 

            var newPos = streamLength + pos;

            
            _logger.LogInformation("Stream Position" + newPos.ToString());

            _memoryCache.Set(section,newPos);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex.Message);
        }

        await outputStream.FlushAsync();
     }

    [Route("startTestRun/{template}")]
    [HttpPost]
    public async Task<string> StartTestRun(string template)
     {
         var instanceName  = template +"-"+ Guid.NewGuid();
         var command = $"jmeter -n -t /opt/testfile/{template} -l /opt/testfile/{instanceName}.csv";
        var commands = command.Split(null).ToList();
                DockerMethod(commands);     

                return instanceName;
    }


    [HttpPost]
     public async Task<IActionResult> Post()
     {
        using (var reader = new StreamReader(Request.Body))
            {
                var body = await reader.ReadToEndAsync();

                var commands = body.Split(null).ToList();
                DockerMethod(commands);
            }
                return Ok();
                
     }


    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        
        // System.IO.File.WriteAllText("/home/node/app/" + "test.txt", 
        //           "test");

                  var testFile = System.IO.File.ReadAllText(_hostingEnvironment.ContentRootPath + "/" + "test.jmx");
                        System.IO.File.WriteAllText("/home/node/app/" + "test.jmx", 
                  testFile);
                  
                  setRedis();
                //   DockerMethod("jmeter -n -t /opt/testfile/test.jmx -l /opt/testfile/result1.csv");
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(55, 55),
            Summary = LinuxCommandLine()
        })
        .ToArray();
    }
    
    private  void DockerMethod(List<string> command)
        {
                        _logger.LogInformation("Test");


            var uri = new Uri("npipe://./pipe/docker_engine");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                uri = new Uri("unix:///var/run/docker.sock");
            }

            // uri = new Uri("tcp://docker.for.win.localhost:2375");
            DockerClient client = new DockerClientConfiguration(uri)
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
                            Cmd = command
                        }).Result;

                    // When

                    Task.Run(()=>
                    {
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
                    });
                }
            }
        }

    private void setRedis()
    {
        var options = ConfigurationOptions.Parse("redis-master:6379,abortConnect=false,connectTimeout=30000,responseTimeout=30000"); // host1:port1, host2:port2, ...
        options.Password = "MyPassword";      
        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(options);
        IDatabase conn = redis.GetDatabase();
        var test = conn.Database;

        conn.StringAppend("test","abc");
    }

    private string LinuxCommandLine()
    {
        string command = "pwd";
        string result = "";
        using (System.Diagnostics.Process proc = new System.Diagnostics.Process())
        {
            proc.StartInfo.FileName = "/bin/bash";
            proc.StartInfo.Arguments = "-c \" " + command + " \"";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.Start();

            result += proc.StandardOutput.ReadToEnd();
            result += proc.StandardError.ReadToEnd();

            proc.WaitForExit();
        }
        return result;
    }
}
