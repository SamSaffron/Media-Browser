using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DvrmsMetadataProvider.Metadata {
    public enum WMPictureType {
        Unknown = 0, //Picture of a type not specifically listed in this table
        PNG = 1, //32 pixel by 32 pixel file icon. Use only with portable network graphics (PNG) format
        FileIcon = 2, //File icon not conforming to type 1 above
        FrontAblumCover = 3, //Front album cover
        BackAlbumCover = 4, //Back album cover
        Leaflet = 5, //Leaflet page
        Media = 6, //Media. Typically this type of image is of the label side of a CD
        LeadArtist = 7, //Picture of the lead artist, lead performer, or soloist
        OtherArist = 8, //Picture of one of the artists or performers
        Conductor = 9, //Picture of the conductor
        Band = 10, //Picture of the band or orchestra
        Composer = 11, //Picture of the composer
        Lyricist = 12, //Picture of the lyricist or writer
        Studio = 13, //Picture of the recording studio or location
        RecordingSession = 14, //Picture taken during a recording session
        Performance = 15, //Picture taken during a performance
        ScreenCapture = 16, //Screen capture from a movie or video
        Fish = 17, //A bright colored fish
        Illustration = 18, //Illustration
        BandLogo = 19, //Logo of the band or artist
        StudioLogo = 20 //Logo of the publisher or studio
    }
}
