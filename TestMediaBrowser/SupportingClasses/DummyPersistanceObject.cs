using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MediaBrowser.Library.Persistance;

namespace TestMediaBrowser.SupportingClasses {
    [Serializable]
    public class DummyPersistanceObject {

        [Persist]
        public int Bar1;
        [Persist]
        public string Bar2;
        [Persist]
        Guid bar3 = Guid.NewGuid();
        public Guid Bar3 { get { return bar3; } }
        [Persist]
        public String Null;

        public void Write(BinaryWriter bw) {
            bw.Write(Bar1);
            bw.SafeWriteString(Bar2);
            bw.Write(Bar3.ToByteArray());
            bw.SafeWriteString(Null);
        }

        public static DummyPersistanceObject Read(BinaryReader br) {
            DummyPersistanceObject f = new DummyPersistanceObject();
            f.Bar1 = br.ReadInt32();
            f.Bar2 = br.SafeReadString();
            f.bar3 = new Guid(br.ReadBytes(16));
            f.Null = br.SafeReadString();
            return f;
        }


        public override int GetHashCode() {
            unchecked {
                return Bar1.GetHashCode()
                    + Bar3.GetHashCode()
                    + (Bar2 ?? "").GetHashCode() + (Null ?? "").GetHashCode();
            }
        }

        public override bool Equals(object obj) {
            DummyPersistanceObject other = obj as DummyPersistanceObject;
            if (other != null) {
                return Bar1 == other.Bar1 && Bar2 == other.Bar2
                    && Bar3 == other.Bar3
                    && this.Null == other.Null;
            }
            return false;
        }
    }
}
