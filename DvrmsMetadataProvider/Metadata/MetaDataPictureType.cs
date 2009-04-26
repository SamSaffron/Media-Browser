using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace DvrmsMetadataProvider.Metadata {
    public class MetaDataPicture : IDisposable {
        public Image Picture;
        public WMPictureType PictureType;
        public string Description;
        private bool _disposed;

        public MetaDataPicture() { }

        public MetaDataPicture(Image picture, WMPictureType pictureType, string description) {
            this.Picture = picture;
            this.PictureType = pictureType;
            this.Description = description;
        }

        #region IDisposable Members

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing) {
            if (disposing) {
                if (!_disposed) {
                    if (Picture != null)
                        Picture.Dispose();
                    _disposed = true;
                }
            }
        }

        #endregion
    }
}
