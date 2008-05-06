using System;
using System.Collections.Generic;
using System.Text;

namespace SamSoft.VideoBrowser.LibraryManagement
{
    // Bodgy class used to get around input/output MCML parmeter maddness
    public class FolderItemContainer
    {
        private IFolderItem _object = new FolderItem();

        public IFolderItem Value
        {
            get { return _object; }
            set { _object = value; }
        }
	
    }
}
