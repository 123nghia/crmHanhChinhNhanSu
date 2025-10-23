
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;



namespace Human.CDN2.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class Filecontroller : Controller
    {


        public Filecontroller()
        {

        }




        [AllowAnonymous]
        [HttpGet("~/api/file/getaudio9")]
        public async Task<FileResult> getaudio9(string? filePath)
        {

            //await _campagnBusiness.ResetCase();
            string urlDow = "http://192.168.1.9:3002/api/getFileAudio?filePath=" + filePath;

            using (var net = new System.Net.WebClient())
            {
                var fileName = Path.GetFileName(filePath);

                byte[] data;
                try
                {
                    data = net.DownloadData(urlDow);
                    return File(data, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);

                }
                catch (Exception)
                {
                    return null;
                }
            }
            return null;

        }

        [AllowAnonymous]
        [HttpGet("~/api/file/getaudio12")]
        public async Task<FileResult> getaudio12(string? filePath)
        {

            string urlDow = "http://192.168.1.12:3002/api/getFileAudio?filePath=" + filePath;

            using (var net = new System.Net.WebClient())
            {
                var fileName = Path.GetFileName(filePath);

                byte[] data;
                try
                {
                    data = net.DownloadData(urlDow);
                    return File(data, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
                }
                catch (Exception)
                {

                    try
                    {
                        urlDow = "http://192.168.1.151:3002/api/getFileAudio?filePath=" + filePath;
                        data = net.DownloadData(urlDow);
                        return File(data, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
                    }
                    catch (Exception)
                    {

                        return null;
                    }


                }
            }
            return null;

        }


        [AllowAnonymous]
        [HttpGet("~/api/file/getaudio09")]
        public async Task<FileResult> getaudio09(string? filePath)
        {


            //await _campagnBusiness.ResetCase();
            string urlDow = "http://192.168.1.9:3002/api/getFileAudio?filePath=" + filePath;

            using (var net = new System.Net.WebClient())
            {
                var fileName = Path.GetFileName(filePath);

                byte[] data;
                try
                {
                    data = net.DownloadData(urlDow);
                    return File(data, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
                }
                catch (Exception)
                {
                    return null;

                }
            }
            return null;

        }

        [AllowAnonymous]
        [HttpGet("~/api/file/getaudio151")]
        public async Task<FileResult> getaudio151(string? filePath)
        {


            //await _campagnBusiness.ResetCase();
            string urlDow = "http://192.168.1.151:3002/api/getFileAudio?filePath=" + filePath;

            using (var net = new System.Net.WebClient())
            {
                var fileName = Path.GetFileName(filePath);

                byte[] data;
                try
                {
                    data = net.DownloadData(urlDow);
                    return File(data, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
                }
                catch (Exception)
                {
                    return null;

                }
            }
            return null;

        }
    }
}