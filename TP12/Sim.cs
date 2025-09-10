using MathNet.Numerics.Distributions;
using Newtonsoft.Json.Linq;

namespace TP12
{
    internal class Sim
    {
        JObject Control;
        const string pathControl = "VariablesControl.json";
        Func<double> _FDP_IA;

        #region FDPS (valores fitter)
        // FDP Monto 
        const double FDP_monto_scale = 12984.48920148089;
        const double FDP_monto_loc = 1000;
        static Exponential _FDP_monto = new Exponential(1 / FDP_monto_scale);

        // FDP arribos 0 a 9 hs
        const double FDP_IAT0_loc = 10000.0;
        const double FDP_IAT0_scale = 149006.50258876648;
        static Exponential _FDP_IAT0 = new Exponential(1 / FDP_IAT0_scale);

        // FDP arribos 9 a 16 hs
        const double FDP_IAT9_s = 0.6994056635013702;
        const double FDP_IAT9_loc = 1212.104630014977;
        const double FDP_IAT9_scale = 16556.050972717094;
        static LogNormal _FDP_IAT9 = new LogNormal(Math.Log(FDP_IAT9_scale), FDP_IAT9_s);

        // FDP arribos 16 a 00 hs
        const double FDP_IA16_s = 0.7270381882522544;
        const double FDP_IA16_loc = 579.9744178521454;
        const double FDP_IA16_scale = 19378.32363875735;
        static LogNormal _FDP_IAT16 = new LogNormal(Math.Log(FDP_IA16_scale), FDP_IA16_s);

        // FDP tiempo atencion tx enviadas al emisor
        const double FDP_TAE_loc = 5153880;
        const double FDP_TAE_scale = 2303582.325727702;
        static Laplace _FDP_TAE = new Laplace(FDP_TAE_loc, FDP_TAE_scale);

        // FDP tiempo atencion tx resueltas por adquiriente
        const double FDP_TAA_a = 2.06647899667313;
        const double FDP_TAA_b = 154524788.8009786;
        const double FDP_TAA_loc = 24578.029289443984;
        const double FDP_TAA_scale = 28437213135562.016;
        static Beta _FDP_TAA = new Beta(FDP_TAA_a, FDP_TAA_b);

        #endregion


        const double HV = double.MaxValue;

        #region vars

        public static double T = 0;
        ///FDPs en us, 10e6
        /// 24hs = 24 * 3600 s
        double TF = 10 * 3600 * 10e6;
        double TPIT = 0;
        int it = 0;

        #endregion

        MarcaTarjeta VISA;
        MarcaTarjeta AMEX;
        MarcaTarjeta MASTER;

        #region estaticos
        public static int ContadorTerminales = 0;
        public static int TerminalesMax;
        public static int RechazadosPorFaltaTerminales = 0;
        public static double SumaMontoRechazadosTerminales = 0;
        public static int RechazadosAprobacionSistema = 0;
        #endregion
        public void Configurar()
        {

            Control = JObject.Parse(File.ReadAllText(pathControl));
            var horario = Control["RANGO_HORARIO"].Value<string>();
            switch (horario)
            {
                case "0":
                    _FDP_IA = FDP_IAT0;
                    PORC_ECOMERCE = PORC_ECOMERCE_0;
                    break;
                case "9":
                    _FDP_IA = FDP_IAT9;
                    PORC_ECOMERCE = PORC_ECOMERCE_9;
                    break;
                case "16":
                    _FDP_IA = FDP_IAT16;
                    PORC_ECOMERCE = PORC_ECOMERCE_16;
                    break;
            }
            VISA = new MarcaTarjeta(Control["CANT_PROCESOS_VISA"].Value<int>());
            AMEX = new MarcaTarjeta(Control["CANT_PROCESOS_AMEX"].Value<int>());
            MASTER = new MarcaTarjeta(Control["CANT_PROCESOS_MASTER"].Value<int>());
            TerminalesMax = Control["CANT_ID_TERMINALES"].Value<int>();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Comenzando simulacion");
            Console.ForegroundColor = ConsoleColor.White;
            foreach (var par in Control)
                Console.WriteLine($"{par.Key} = {par.Value}");
            Console.WriteLine("");

        }
        (int x, int y) cursor;

        double step;
        int steps = 40;
        public void Iniciar()
        {
            it = 0;

            cursor = Console.GetCursorPosition();
            step = TF / steps;

            while (T < TF)
            {
                T = TPIT;
                TPIT += FDP_IA();

                Progreso(T);


                switch (RandomMarca())
                {
                    case NombreTarjeta.Visa: VISA.Ejecutar(T); break;
                    case NombreTarjeta.Amex: AMEX.Ejecutar(T); break;
                    case NombreTarjeta.Master: MASTER.Ejecutar(T); break;
                }
                it++;
            }

            //for (int i = 0; i < 100; i++)
            //    Console.WriteLine(F2(FDP_Monto()));
            Console.WriteLine();
            Resultados();

            Console.Read();
        }
        string prevString = "";
        void Progreso(double T)
        {
            string str = "Progreso [";
            for (int i = 0; i < steps; i++)
            {
                str += T >= step * i ? "#" : "-";
            }
            str += "]\n";

            if (str != prevString)
            {
                prevString = str;

                Console.SetCursorPosition(cursor.x, cursor.y);
                Console.Write(str);
            }


        }
        private void Resultados()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Fin en T {(int)(T / 10e6)} segundos simulados ({it} iteraciones)");

            var NT = VISA.N + AMEX.N + MASTER.N;
            var pV = F2((float)VISA.N * 100 / NT);
            var pA = F2((float)AMEX.N * 100 / NT);
            var pM = F2((float)MASTER.N * 100 / NT);

            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine($"Resultados de VISA ({pV}% TX)");
            VISA.Resultados();
            Console.WriteLine($"Resultados de AMEX ({pA}% TX)");
            AMEX.Resultados();
            Console.WriteLine($"Resultados de MASTER ({pM}% TX)");
            MASTER.Resultados();


            var sumaEcomerce = VISA.ContadorEcomerce + AMEX.ContadorEcomerce + MASTER.ContadorEcomerce;
            var pRTER = RechazadosPorFaltaTerminales / sumaEcomerce;

            Console.Write("Porcentaje de rechazados por falta de terminales: ");
            Console.Write($"{ValorConColorPorcentaje(pRTER)} %\n");
            Console.ForegroundColor = ConsoleColor.White;

            var pMRT = SumaMontoRechazadosTerminales * 10e6 / T;

            Console.Write("Promedio diario de Monto de rechazados por falta de terminales : ");
            Console.Write($"{ValorConColorPorcentaje(pMRT)} [$ por seg]\n");
            Console.ForegroundColor = ConsoleColor.White;

            Console.ReadLine();

        }

        #region FDP funciones
        public static double FDP_Monto()
        {
            return _FDP_monto.Sample() + FDP_monto_loc;
        }

        double FDP_IA()
        {
            return _FDP_IA.Invoke();
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

        public static double FDP_TAE()
        {
            return _FDP_TAE.Sample();
        }
        public static double FDP_TAA()
        {
            return _FDP_TAA.Sample() * FDP_TAA_scale + FDP_TAA_loc;
        }
        #endregion

        #region subrutinas

        const double PORC_MASTER = 0.26;
        const double PORC_AMEX = 0.01;
        private NombreTarjeta RandomMarca()
        {
            var r = new Random().NextDouble();

            if (r < PORC_AMEX)
                return NombreTarjeta.Amex;
            if (r < PORC_MASTER)
                return NombreTarjeta.Master;

            return NombreTarjeta.Visa;
        }
        const double PORC_ECOMERCE_0 = 0.53;
        const double PORC_ECOMERCE_9 = 0.19;
        const double PORC_ECOMERCE_16 = 0.32;
        static double PORC_ECOMERCE;
        public static TipoTX RandomTipoTX()
        {
            return Random() < PORC_ECOMERCE ? TipoTX.Ecomerce : TipoTX.Presente;
        }

        const double PORC_DEBITO = 0.62;
        const double PORC_A_COBRAR_DEBITO = 0.01;
        const double PORC_A_COBRAR_CREDITO = 0.018;
        public static double RandomPorcACobrar()
        {
            return Random() < PORC_DEBITO ? PORC_A_COBRAR_DEBITO : PORC_A_COBRAR_CREDITO;
        }


        const double PORC_ENVIADO_EMISOR = 0.98;
        public static Resolucion RandomResolucionTransaccion()
        {
            return Random() < PORC_ENVIADO_EMISOR ? Resolucion.Emisor : Resolucion.Adquiriente;
        }


        static Random r = new Random();
        public static double Random()
        {
            return r.NextDouble();
        }


        #endregion
        public static string F2(double val)
        {
            return val.ToString("F2");
        }
        public static string ValorConColorPorcentaje(double porcentaje, bool mayorMejor = false)
        {
            var color = ConsoleColor.White;
            if (porcentaje < 0.25)
                color = mayorMejor ? ConsoleColor.Red : ConsoleColor.Cyan;
            else if (porcentaje < 0.50)
                color = mayorMejor ? ConsoleColor.Yellow : ConsoleColor.Green;
            else if (porcentaje < 0.75)
                color = mayorMejor ? ConsoleColor.Green : ConsoleColor.Yellow;
            else
                color = mayorMejor ? ConsoleColor.Cyan : ConsoleColor.Red;

            Console.ForegroundColor = color;
            return $"{F2(porcentaje * 100)}";
        }



    }

    enum NombreTarjeta
    {
        Visa, Amex, Master
    }
    enum TipoTX
    {
        Ecomerce,
        Presente
    }
    enum Resolucion
    {
        Emisor,
        Adquiriente
    }
}
