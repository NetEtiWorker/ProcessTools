using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetEti.ApplicationControl
{
    /// <summary>
    /// Parameter für den Threader-Konstruktor
    /// </summary>
    public class ThreadParameter
    {
        /// <summary>
        /// Außerhalb erzeugtes CanellationToken für einen
        /// kontrollierten (cooperativen) Abbruch eines Threads.
        /// </summary>
        public CancellationToken Token { get; set; }

        /// <summary>
        /// Zusätzlicher Name für den Thread.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Default: true.
        /// ACHTUNG: nur für Testzwecke nutzen, kann nur nach Instanziierung
        /// über die Property gesetzt werden!
        /// Eine asynchrone Methode kann zu Testzwecken auf diesen Schalter reagieren;
        /// das muss dort aber vorgesehen werden (Bei true könnte sich die Methode auf
        /// eine externe Abbruchanforderung, welche über 'Token' übermittelt würde, beenden).
        /// </summary>
        public bool IsCooperativeCancellingEnabled { get; set; }

        /// <summary>
        /// Konstruktor - erwartet ein CancellationToken.
        /// Es kann zusätzlich ein Name mitgegeben werden.
        /// </summary>
        public ThreadParameter(CancellationToken token, string? name = null)
        {
            this.IsCooperativeCancellingEnabled = true;
            this.Token = token;
            this.Name = name;
        }
    }

}
