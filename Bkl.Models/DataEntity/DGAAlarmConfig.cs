using System.Text.Json.Serialization;

namespace Bkl.Models
{
    public class DGAAlarmConfig
    {
        public static DGAAlarmConfig Default = new DGAAlarmConfig
        {
            OilBulk = 15,

            H2 = 150,
            C2H2 = 5,
            TotHyd = 150,

            CO = 0,
            CO2 = 0,
            H2ar = 10,
            C2H2ar = 0.2,
            TotHydar = 12,


            COar = 100,
            CO2ar = 200,
            AbsMulTh = 0.4,
            GasMulTh = 0.6,
            MulTh = 0.8,
        };
        
        public double OilBulk { get; set; }
        
        public double H2 { get; set; }
        
        public double H2ar { get; set; }
        
        public double C2H2 { get; set; }
        
        public double C2H2ar { get; set; }
        
        public double TotHyd { get; set; }
        
        public double TotHydar { get; set; }
        
        public double CO { get; set; }
        
        public double COar { get; set; }
        
        public double CO2 { get; set; }
        
        public double CO2ar { get; set; }
        
        public double AbsMulTh { get; set; }
        
        public double GasMulTh { get; set; }
        
        public double MulTh { get;  set; }
    }

}
