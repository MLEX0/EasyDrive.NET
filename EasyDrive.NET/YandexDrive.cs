using Newtonsoft.Json;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using YandexDisk.Client.Http;
using YandexDisk.Client.Protocol;

namespace EasyDrive
{
    public class YandexDrive
    {
        public string Token { get; set; }
        private DiskHttpApi _yclient;
        private HttpClient _client;

        public YandexDrive(string token) 
        {
            Token = token;
            _yclient = new DiskHttpApi(Token);
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<UploadFile> UploadFileAsync(string filePath, string newFileName, string diskFolderName)
        {
            //Получаем информацию о корневой папке
            var RootFolderData = await _yclient.MetaInfo.GetInfoAsync(new YandexDisk.Client.Protocol.ResourceRequest()
            {
                Path = "/"
            });

            //Проверяем на наличие папки для картинок
            if (!RootFolderData.Embedded.Items.Any(i => i.Type == ResourceType.Dir
            && i.Name.Equals(diskFolderName)))
            {
                await _yclient.Commands.CreateDictionaryAsync("/" + diskFolderName);
            }

            UploadFile file = new UploadFile();

            string strLink = "disk:/"
            + diskFolderName
            + "/"
            + newFileName;

            var UploadLink = await _yclient.Files.GetUploadLinkAsync(strLink, overwrite: false);

            using (var fs = System.IO.File.OpenRead(filePath))
            {
                await _yclient.Files.UploadAsync(UploadLink, fs);
            }

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Token);
            Link link;
            HttpResponseMessage response = await _client.GetAsync($"https://cloud-api.yandex.net/v1/disk/resources/download?path={strLink}");
            var json = await response.Content.ReadAsStringAsync();
            link = JsonConvert.DeserializeObject<Link>(json);
            file.DownLoadLink = link.href;
            file.DrivePath = strLink;
            file.NewName = newFileName;

            return file;
        }

        public byte[] DownloadFile(string url)
        {
            using (WebClient webClient = new WebClient())
            {
                byte[] file = webClient.DownloadData(url);
                return file;
            }
        }

        public async Task DeleteFile(string drivePath)
        {
            DeleteFileRequest deleteFileRequest = new DeleteFileRequest();
            deleteFileRequest.Permanently = true;
            deleteFileRequest.Path = drivePath;
            await _yclient.Commands.DeleteAsync(deleteFileRequest);
        }

    }
}