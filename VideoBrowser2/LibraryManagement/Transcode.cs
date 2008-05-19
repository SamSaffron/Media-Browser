using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using Transcode360.Interface;
using Microsoft.MediaCenter;
using Microsoft.MediaCenter.UI;

namespace SamSoft.VideoBrowser.LibraryManagement
{
    public static class Transcode
    {
        private static ITranscode360 myServer = null;
        private static TcpClientChannel myChannel = null;
        public static ITranscode360 ConnectToTranscoder()
        {
            if (myServer == null)
            {
                Hashtable properties = new Hashtable();
                properties.Add("name", "");
                myChannel = new TcpClientChannel(properties, null);
                ChannelServices.RegisterChannel(myChannel);

                myServer = (ITranscode360)Activator.GetObject(typeof(ITranscode360),
                   "tcp://localhost:1401/RemotingServices/Transcode360");
                return myServer;
            }
            else
            {
                return myServer;
            }
        }

        public static string BeginTranscode(string filename)
        {
            string bufferpath = null;

            // Check if the transcode is already completed
            if (myServer.IsMediaTranscodeComplete(filename, 0, out bufferpath))
            {
                return bufferpath;
            }

            // the file is already being transcoded (or is already done)
            if (myServer.IsMediaTranscoding(filename, 0, out bufferpath))
            {
                return bufferpath;
            }

            // Otherwise we need to start the transcode
            if (myServer.Transcode(filename, out bufferpath, 0))
            {
                return bufferpath;
            }
            return bufferpath;
        }
       
        public static bool DisconnectTranscoder()
        {
            if (myChannel != null)
            {
                ChannelServices.UnregisterChannel(myChannel);    
            }
            return true;
        }

    }
}