using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection.Emit;
using System.Reflection;
using System.Diagnostics;
using System.Linq.Expressions;

namespace MediaBrowser.Library.Persistance {

    public class Serializer {
        static Dictionary<string, Type> typeMap = new Dictionary<string, Type>();
        static Dictionary<Type, Serializer> serializers = new Dictionary<Type, Serializer>();

        private static Serializer GetSerializer(Type type)
        {
            Serializer serializer;
            if (!serializers.TryGetValue(type, out serializer)) {
                Type baked = typeof(GenericSerializer<>).MakeGenericType(type);

                object persistables = baked.GetProperty("Persistables", BindingFlags.NonPublic | BindingFlags.Static).GetGetMethod(true).Invoke(null, null); 
               
                serializer = new Serializer((IEnumerable<Persistable>)persistables,type);

                serializers[type] = serializer;
            }
            return serializer;
        }

        /// <summary>
        /// Serialize any object to a stream, will write a type manifest as well 
        /// </summary>
        public static void Serialize<T>(Stream stream, T obj) where T : class, new() {
            Serialize(new BinaryWriter(stream), obj);
        }

        /// <summary>
        /// Serialize any object to a stream, will write a type manifest as well 
        /// </summary>
        public static void Serialize<T>(BinaryWriter bw, T obj) where T : class, new() {
            if (obj == null) {
                throw new ArgumentNullException("object being serialized can not be null"); 
            }

            Type type = obj.GetType();

            bw.Write(type.FullName);

            MethodInfo method;
            // Build in versioning data here... 
            if (Persistable.TryGetBinaryWrite(type, out method)) {
                if (method.IsStatic) {
                    method.Invoke(null, new object[] {bw, obj });
                } else {
                    method.Invoke(bw, new object[] { obj });
                }
            } else {
                if (typeof(T) == type) {
                    GenericSerializer<T>.Serialize(obj, bw);
                } else {
                    // slower
                    Serializer.GetSerializer(type).SerializeInternal(obj, bw);
                }
            }

          
        }

        public static T Deserialize<T>(Stream stream) where T : class, new() {
            return Deserialize<T>(new BinaryReader(stream));
        }

        /// <summary>
        /// Deserialize an object that was serialized using the Serialize method,
        ///  has robustness to version changes 
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static T Deserialize<T>(BinaryReader reader) where T : class, new() { 
            Type type = GetCachedType(reader.ReadString());

            T deserialized;

            MethodInfo method;
            // versioning goes here 
            if (Persistable.TryGetBinaryRead(type, out method)) {
                
                if (method.IsStatic) {
                    deserialized = (T)method.Invoke(null, new object[] { reader } );
                } else {
                    deserialized = (T)method.Invoke(reader, null);
                }
            } else {
                if (typeof(T) == type) {
                    deserialized = GenericSerializer<T>.Deserialize(reader);
                } else {
                    deserialized = (T)Serializer.GetSerializer(type).DeserializeInternal(reader);
                }
            }

            return deserialized;
        } 


        internal static Type GetCachedType(string typeName) {
            Type type;
            if (!typeMap.TryGetValue(typeName, out type)) {
                type = AppDomain
                    .CurrentDomain
                    .GetAssemblies()
                    .Select(a => a.GetType(typeName, false))
                    .Where(t => t != null)
                    .FirstOrDefault();
                if (type != null) typeMap[typeName] = type;
            }
            return type;
        }

        public static T Clone<T>(T obj) where T : class, new() {
            T rval;

            // tricky, the T passed in may not be actual type of the object being cloned 
            Serializer serializer = GetSerializer(obj.GetType());

            using (var stream = new MemoryStream()) {
                var writer = new BinaryWriter(stream);
                serializer.SerializeInternal(obj, writer);
                stream.Position = 0;
                var reader = new BinaryReader(stream);
                rval = (T)serializer.DeserializeInternal(reader);
            }
            return rval;
        }


        Persistable[] persistables;
        Type type;
        Func<object> constructor;

        private Serializer(IEnumerable<Persistable> persistables, Type type) {
            this.type = type;
            this.persistables = persistables.ToArray();

            DynamicMethod dm = new DynamicMethod("FastConstruct", type,
                Type.EmptyTypes, typeof(Serializer).Module, true);

            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Ret);
            constructor = (Func<object>)dm.CreateDelegate(typeof(Func<object>));
        }


        private void SerializeInternal(object data, BinaryWriter bw) {
            for (int i = 0; i < persistables.Length; i++) {
                persistables[i].Serialize(bw, data);
            }
        }
 

        private object DeserializeInternal(BinaryReader br) {
            try {
                object obj = constructor.DynamicInvoke();

                for (int i = 0; i < persistables.Length; i++) {
                    persistables[i].Deserialize(br, obj);
                }

                return obj;
            } catch (Exception exception) {
                throw new SerializationException("Failed to deserialize object, corrupt stream.", exception);
            }
        }

        public void MergeObjects(object source, object target, bool force) {
            foreach (var persistable in persistables) {
                if (persistable.GetValue(target) == null || force) {
                    persistable.SetValue(target, persistable.GetValue(source));
                }
            }
        }

        /// <summary>
        /// Merge all non-null persistable fields in source into target
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public static void Merge(object source, object target) {
            Merge(source, target, false);
        }

        /// <summary>
        /// Merge persistable fields in source into target
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="force">force non null fields to be overwritten</param>
        public static void Merge(object source, object target, bool force) {
            GetSerializer(source.GetType()).MergeObjects(source, target, force);
        }

        internal static IEnumerable<Persistable> GetPersistables(object obj) {
            return GetSerializer(obj.GetType()).persistables;
        }
  
    }

    
}
