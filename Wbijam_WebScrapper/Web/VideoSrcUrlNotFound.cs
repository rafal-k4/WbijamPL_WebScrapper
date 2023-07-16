using System.Runtime.Serialization;

namespace Wbijam.WebScrapper.Web
{
    [Serializable]
    internal class VideoSrcUrlNotFound : Exception
    {
        public VideoSrcUrlNotFound()
        {
        }
    }
}