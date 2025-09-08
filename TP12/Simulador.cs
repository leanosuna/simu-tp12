using MathNet.Numerics.Distributions;
using Newtonsoft.Json.Linq;

namespace TP12
{
    internal class Simulador
    {
        JObject Control;
        const string pathControl = "VariablesControl.json";
        Func<double> _FDP_SIM;

        #region FDPS (valores fitter)
        // FDP Monto 
        const double FDP_monto_scale = 12984.48920148089;
        const double FDP_monto_loc = 1000;
        Exponential _FDP_monto = new Exponential(1 / FDP_monto_scale);

        // FDP arribos 0 a 9 hs
        const double FDP_IAT0_loc = 10000.0;
        const double FDP_IAT0_scale = 149006.50258876648;
        Exponential _FDP_IAT0 = new Exponential(1 / FDP_IAT0_scale);

        // FDP arribos 9 a 16 hs
        const double FDP_IAT9_s = 0.6994056635013702;
        const double FDP_IAT9_loc = 1212.104630014977;
        const double FDP_IAT9_scale = 16556.050972717094;
        LogNormal _FDP_IAT9 = new LogNormal(Math.Log(FDP_IAT9_scale), FDP_IAT9_s);

        // FDP arribos 16 a 00 hs
        const double FDP_IA16_s = 0.7270381882522544;
        const double FDP_IA16_loc = 579.9744178521454;
        const double FDP_IA16_scale = 19378.32363875735;
        LogNormal _FDP_IAT16 = new LogNormal(Math.Log(FDP_IA16_scale), FDP_IA16_s);

        const double FDP_TAE_loc = 5153880;
        const double FDP_TAE_scale = 2303582.325727702;
        Laplace _FDP_TAE = new Laplace(FDP_TAE_loc, FDP_TAE_scale);

        #endregion
        const double HV = double.MaxValue;

        #region vars

        double T = 0;
        double TF = 1000;
        double TPIT = 0;

        #endregion
        public void Configurar()
        {

            Control = JObject.Parse(File.ReadAllText(pathControl));
            var horario = Control["RANGO_HORARIO"].Value<string>();
            switch (horario)
            {
                case "0":
                    _FDP_SIM = FDP_IAT0; break;
                case "9":
                    _FDP_SIM = FDP_IAT9; break;
                case "16":
                    _FDP_SIM = FDP_IAT16; break;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Comenzando simulacion");
            foreach (var par in Control)
                Console.WriteLine($"{par.Key} = {par.Value}");
            Console.WriteLine("");

            Console.ForegroundColor = ConsoleColor.White;
        }
        public void Iniciar()
        {
            while (T < TF)
            {
                Console.WriteLine($"T {T} {F2(FDP_IA())}");
                T++;
            }
            Resultados();

            Console.Read();
        }

        public void Resultados()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Fin en T {T} ");
        }

        double FDP_Monto()
        {
            return _FDP_monto.Sample() + FDP_monto_loc;
        }

        double FDP_IA()
        {
            return _FDP_SIM.Invoke();
        }

        double FDP_IAT0()
        {
            return _FDP_IAT0.Sample() + FDP_IAT0_loc;
        }

        double FDP_IAT9()
        {
            return _FDP_IAT9.Sample() + FDP_IAT9_loc;
        }
        double FDP_IAT16()
        {
            return _FDP_IAT16.Sample() + FDP_IA16_loc;
        }

        double FDP_TAE()
        {
            return _FDP_TAE.Sample();
        }

        public string F2(double val)
        {
            return val.ToString("F2");
        }




    }
}
