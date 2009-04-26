// Stephen Toub
// stoub@microsoft.com

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;

using Toub.MediaCenter.Dvrms.DirectShow;
using DvrmsMetadataProvider.DirectShow;
using System.Drawing;
using DvrmsMetadataProvider.Metadata;

namespace Toub.MediaCenter.Dvrms.Metadata
{
	/// <summary>Metadata editor for DVR-MS files.</summary>
	public sealed class DvrmsMetadataEditor : MetadataEditor
	{
		IStreamBufferRecordingAttribute _editor;

		/// <summary>Initializes the editor.</summary>
		/// <param name="filepath">The path to the file.</param>
		public DvrmsMetadataEditor(string filepath) : base()
		{
			IFileSourceFilter sourceFilter = (IFileSourceFilter)ClassId.CoCreateInstance(ClassId.RecordingAttributes);
			sourceFilter.Load(filepath, null);
			_editor = (IStreamBufferRecordingAttribute)sourceFilter;
		}

        public MetaDataPicture GetWMPicture() {
            if (_editor == null) throw new ObjectDisposedException(GetType().Name);

            WMPicture picture = new WMPicture();
            IntPtr pictureParam = IntPtr.Zero;
            MetadataItemType attributeType;
            //byte[] attributeValue = null;
            short attributeValueLength = 0;

            try {
                _editor.GetAttributeByName("WM/Picture", 0, out attributeType, pictureParam, ref attributeValueLength);

                if (attributeValueLength != 0) {
                    //attributeValue = new byte[attributeValueLength];
                    pictureParam = Marshal.AllocHGlobal(attributeValueLength);

                    _editor.GetAttributeByName("WM/Picture", 0, out attributeType, pictureParam, ref attributeValueLength);

                    //pictureParam = Marshal.AllocHGlobal(attributeValueLength);
                    //Marshal.Copy(attributeValue, 0, pictureParam, attributeValueLength);
                    picture = (WMPicture)Marshal.PtrToStructure(pictureParam, typeof(WMPicture));

                    byte[] wmPic = new byte[picture.dwDataLen];
                    Marshal.Copy(picture.pbData, wmPic, 0, picture.dwDataLen);
                    string description = Marshal.PtrToStringUni(picture.pwszDescription);
                    string mimeType = Marshal.PtrToStringUni(picture.pwszMIMEType);

                    ImageConverter imageConverter = new ImageConverter();
                    Image iPic = (Image)imageConverter.ConvertFrom(wmPic);

                    return new MetaDataPicture(iPic, (WMPictureType)picture.bPictureType, description);
                } else {
                    return null;
                }
            } catch (COMException ex) {
                int HR = Marshal.GetHRForException(ex);
                Marshal.ThrowExceptionForHR(HR);
                return null;
            } finally {
                if (pictureParam != IntPtr.Zero)
                    Marshal.FreeHGlobal(pictureParam);
                pictureParam = IntPtr.Zero;
            }
        }

		/// <summary>Gets all of the attributes on a file.</summary>
		/// <returns>A collection of the attributes from the file.</returns>
		public override System.Collections.IDictionary GetAttributes()
		{
			if (_editor == null) throw new ObjectDisposedException(GetType().Name);

			Hashtable propsRetrieved = new Hashtable();

			// Get the number of attributes
			ushort attributeCount = _editor.GetAttributeCount(0);

			// Get each attribute by index
			for(ushort i = 0; i < attributeCount; i++)
			{
				MetadataItemType attributeType;
				StringBuilder attributeName = null;
				byte[] attributeValue = null;
				ushort attributeNameLength = 0;
				ushort attributeValueLength = 0;

				// Get the lengths of the name and the value, then use them to create buffers to receive them
				uint reserved = 0;
				_editor.GetAttributeByIndex(i, ref reserved, attributeName, ref attributeNameLength,
					out attributeType, attributeValue, ref attributeValueLength);
				attributeName = new StringBuilder(attributeNameLength);
				attributeValue = new byte[attributeValueLength];

				// Get the name and value
				_editor.GetAttributeByIndex(i, ref reserved, attributeName, ref attributeNameLength,
					out attributeType, attributeValue, ref attributeValueLength);

				// If we got a name, parse the value and add the metadata item
				if (attributeName != null && attributeName.Length > 0)
				{
					object val = ParseAttributeValue(attributeType, attributeValue);
					string key = attributeName.ToString().TrimEnd('\0');
					propsRetrieved[key] = new MetadataItem(key, val, attributeType);
				}
			}

			// Return the parsed items
			return propsRetrieved;
		}

		/// <summary>Sets the collection of string attributes onto the specified file and stream.</summary>
		/// <param name="propsToSet">The properties to set on the file.</param>
		public override void SetAttributes(System.Collections.IDictionary propsToSet)
		{
			if (_editor == null) throw new ObjectDisposedException(GetType().Name);
			if (propsToSet == null) throw new ArgumentNullException("propsToSet");

			byte [] attributeValueBytes;

			// Add each metadata item
			foreach(DictionaryEntry entry in propsToSet)
			{
				// Get the current item and convert it as appropriate to a byte array
				MetadataItem item = (MetadataItem)entry.Value;
				if (TranslateAttributeToByteArray(item, out attributeValueBytes))
				{
					try
					{
						// Set the attribute onto the file
						_editor.SetAttribute(0, item.Name, item.Type, 
							attributeValueBytes, (ushort)attributeValueBytes.Length);
					}
					catch(ArgumentException){}
					catch(COMException) {}
				}
			}
		}

		/// <summary>Release all resources.</summary>
		/// <param name="disposing">Whether this is being called from IDisposable.Dispose.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && _editor != null)
			{
				while(Marshal.ReleaseComObject(_editor) > 0);
				_editor = null;
			}
		}

		[ComImport]
			[Guid("16CA4E03-FE69-4705-BD41-5B7DFC0C95F3")]
			[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
			private interface IStreamBufferRecordingAttribute
		{
			/// <summary>Sets an attribute on a recording object. If an attribute of the same name already exists, overwrites the old.</summary>
			/// <param name="ulReserved">Reserved. Set this parameter to zero.</param>
			/// <param name="pszAttributeName">Wide-character string that contains the name of the attribute.</param>
			/// <param name="StreamBufferAttributeType">Defines the data type of the attribute data.</param>
			/// <param name="pbAttribute">Pointer to a buffer that contains the attribute data.</param>
			/// <param name="cbAttributeLength">The size of the buffer specified in pbAttribute.</param>
			void SetAttribute(
				[In] uint ulReserved, 
				[In, MarshalAs(UnmanagedType.LPWStr)] string pszAttributeName,
				[In] MetadataItemType StreamBufferAttributeType,
				[In, MarshalAs(UnmanagedType.LPArray)] byte [] pbAttribute,
				[In] ushort cbAttributeLength);

			/// <summary>Returns the number of attributes that are currently defined for this stream buffer file.</summary>
			/// <param name="ulReserved">Reserved. Set this parameter to zero.</param>
			/// <returns>Number of attributes that are currently defined for this stream buffer file.</returns>
			ushort GetAttributeCount([In] uint ulReserved);

			/// <summary>Given a name, returns the attribute data.</summary>
			/// <param name="pszAttributeName">Wide-character string that contains the name of the attribute.</param>
			/// <param name="pulReserved">Reserved. Set this parameter to zero.</param>
			/// <param name="pStreamBufferAttributeType">
			/// Pointer to a variable that receives a member of the STREAMBUFFER_ATTR_DATATYPE enumeration. 
			/// This value indicates the data type that you should use to interpret the attribute, which is 
			/// returned in the pbAttribute parameter.
			/// </param>
			/// <param name="pbAttribute">
			/// Pointer to a buffer that receives the attribute, as an array of bytes. Specify the size of the buffer in the 
			/// pcbLength parameter. To find out the required size for the array, set pbAttribute to NULL and check the 
			/// value that is returned in pcbLength.
			/// </param>
			/// <param name="pcbLength">
			/// On input, specifies the size of the buffer given in pbAttribute, in bytes. On output, 
			/// contains the number of bytes that were copied to the buffer.
			/// </param>
			/* Old def
            void GetAttributeByName(
				[In, MarshalAs(UnmanagedType.LPWStr)] string pszAttributeName,
				[In] ref uint pulReserved,
				[Out] out MetadataItemType pStreamBufferAttributeType,
				[Out, MarshalAs(UnmanagedType.LPArray)] byte[] pbAttribute,
				[In, Out] ref ushort pcbLength);

             */
 
            [PreserveSig]
            int GetAttributeByName(
                [In, MarshalAs(UnmanagedType.LPWStr)] string pszAttributeName,
                [In] int pulReserved,
                [Out] out MetadataItemType pStreamBufferAttributeType,
                [In, Out] IntPtr pbAttribute, // BYTE *
                [In, Out] ref short pcbLength
                );

			/// <summary>The GetAttributeByIndex method retrieves an attribute, specified by index number.</summary>
			/// <param name="wIndex">Zero-based index of the attribute to retrieve.</param>
			/// <param name="pulReserved">Reserved. Set this parameter to zero.</param>
			/// <param name="pszAttributeName">Pointer to a buffer that receives the name of the attribute, as a null-terminated wide-character string.</param>
			/// <param name="pcchNameLength">On input, specifies the size of the buffer given in pszAttributeName, in wide characters.</param>
			/// <param name="pStreamBufferAttributeType">Pointer to a variable that receives a member of the STREAMBUFFER_ATTR_DATATYPE enumeration.</param>
			/// <param name="pbAttribute">Pointer to a buffer that receives the attribute, as an array of bytes.</param>
			/// <param name="pcbLength">On input, specifies the size of the buffer given in pbAttribute, in bytes.</param>
			void GetAttributeByIndex (
				[In] ushort wIndex,
				[In, Out] ref uint pulReserved,
				[Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszAttributeName,
				[In, Out] ref ushort pcchNameLength,
				[Out] out MetadataItemType pStreamBufferAttributeType,
				[Out, MarshalAs(UnmanagedType.LPArray)] byte [] pbAttribute,
				[In, Out] ref ushort pcbLength);

			/// <summary>The EnumAttributes method enumerates the existing attributes of the stream buffer file.</summary>
			/// <returns>Address of a variable that receives an IEnumStreamBufferRecordingAttrib interface pointer.</returns>
			[return: MarshalAs(UnmanagedType.Interface)]
			object EnumAttributes();
		}
	}
}