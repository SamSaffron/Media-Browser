/* Brainstorming DVRMS support

using System;
using System.Collections.Generic;
using System.Text;

// metadata reader for dvr ms based off http://blogs.msdn.com/toub/archive/2007/09/22/fun-with-dvr-ms.aspx

namespace SamSoft.VideoBrowser.LibraryManagement
{
    [ComImport]
    [Guid("16CA4E03-FE69-4705-BD41-5B7DFC0C95F3")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IStreamBufferRecordingAttribute
    {
        void SetAttribute(
            [In] uint ulReserved,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszAttributeName,
            [In] MetadataItemType StreamBufferAttributeType,
            [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbAttribute,
            [In] ushort cbAttributeLength);

        ushort GetAttributeCount([In] uint ulReserved);

        void GetAttributeByName(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszAttributeName,
            [In] ref uint pulReserved,
            [Out] out MetadataItemType pStreamBufferAttributeType,
            [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pbAttribute,
            [In, Out] ref ushort pcbLength);

        void GetAttributeByIndex(
            [In] ushort wIndex,
            [In, Out] ref uint pulReserved,
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszAttributeName,
            [In, Out] ref ushort pcchNameLength,
            [Out] out MetadataItemType pStreamBufferAttributeType,
            [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pbAttribute,
            [In, Out] ref ushort pcbLength);

        IntPtr EnumAttributes();
    } 

    class DVRMSItem
    {
         public DvrmsMetadataEditor(string filepath)
    {
        IFileSourceFilter sourceFilter = (IFileSourceFilter)
            ClassId.CoCreateInstance(ClassId.RecordingAttributes);
        sourceFilter.Load(filepath, null);
        _editor = (IStreamBufferRecordingAttribute)sourceFilter;
    }

        public override System.Collections.IDictionary GetAttributes()
        {
            if (_editor == null) throw new ObjectDisposedException(GetType().Name);
            Hashtable propsRetrieved = new Hashtable();
            ushort attributeCount = _editor.GetAttributeCount(0);
            for (ushort i = 0; i < attributeCount; i++)
            {
                MetadataItemType attributeType;
                StringBuilder attributeName = null;
                byte[] attributeValue = null;
                ushort attributeNameLength = 0;
                ushort attributeValueLength = 0;
                uint reserved = 0;

                _editor.GetAttributeByIndex(i, ref reserved, attributeName,
                    ref attributeNameLength, out attributeType, attributeValue, ref attributeValueLength);
                attributeName = new StringBuilder(attributeNameLength);
                attributeValue = new byte[attributeValueLength];
                _editor.GetAttributeByIndex(i, ref reserved, attributeName, ref attributeNameLength,
                    out attributeType, attributeValue, ref attributeValueLength);
                if (attributeName != null && attributeName.Length > 0)
                {
                    object val = ParseAttributeValue(attributeType, attributeValue);
                    string key = attributeName.ToString().TrimEnd('\0');
                    propsRetrieved[key] = new MetadataItem(key, val, attributeType);
                }
            }
            return propsRetrieved;
        } 
    }
}


*/