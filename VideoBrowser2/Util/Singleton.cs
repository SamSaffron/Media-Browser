using System;
using System.Collections.Generic;
using System.Text;

namespace SamSoft.VideoBrowser.Util
{

    public class Singleton<T>  where T : class, new()
    {
        protected virtual void OnInit() { }

        protected Singleton()
        {
            OnInit();
        }

        public static T Instance
        {
            get
            {
                return Nested.Instance;
            }
        }

        class Nested
        {
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit
            static Nested()
            {
            }

            internal static readonly T Instance = new T();
        }
    }
}
