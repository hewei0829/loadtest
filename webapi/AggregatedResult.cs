namespace webapi;
using System.Linq;
public class AggregatedResult
{
    public DateTime date { get; set; }
    
    public string label { 
        get  {return  date.ToString("dd-HH:mm:ss");}
     }

    public long  count { get; set; }

    public AggregatedResult(DateTime date, long count)
    {
        this.date = date;
        this.count  = count;
    }
}


public class AggregatedResults
{
    public List<AggregatedResult> listData {get;set;}

    public long maxValue{get { return listData.Max(x=>x.count);}}
}
