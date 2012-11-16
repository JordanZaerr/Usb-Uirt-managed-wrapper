using System;

namespace UsbUirt.Enums
{
    /// <summary>
    /// Choose which emitter to use to send the IR signal
    /// </summary>
    public enum Emitter
    {
        All = 0,

        /// <summary>
        /// The internal IR blaster
        /// </summary>
        Internal = 1,

        /// <summary>
        /// The external emitter on the 'Right' channel
        /// </summary>
        /// <remarks>
        /// The external connection is a normal headphone jack, that is where the Left/Right channel comes from
        /// </remarks>
        External1 = 2,

        /// <summary>
        /// The external emitter on the 'Left' channel
        /// </summary>
        /// <remarks>
        /// The external connection is a normal headphone jack, that is where the Left/Right channel comes from
        /// </remarks>
        External2 = 3
    }

    internal static class EmitterExtensions
    {
        public static string GetZoneForEmitter(this Emitter emitter)
        {
            switch (emitter)
            {
                case Emitter.Internal:
                    return "Z3";
                case Emitter.External1:
                    return "Z1";
                case Emitter.External2:
                    return "Z2";
                default:
                    return "";
            }
        }
    }
}