﻿using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Liquid.Interfaces;
using Liquid.Repository;
using System.IO;
using System.Threading.Tasks;

namespace Liquid.OnAWS
{
    public class S3 : ILightMediaStorage
    {
        public string Conection { get; set; }
        public string Container { get; set; }
		public dynamic mediaStorageConfiguration { get; set; }

		private static readonly RegionEndpoint bucketRegion = RegionEndpoint.EUWest1;
        private IAmazonS3 _client;
        private TransferUtility _fileTransferUtility;

        public void Initialize()
        {
            _client = new AmazonS3Client(bucketRegion);
            _fileTransferUtility = new TransferUtility(_client);
            WorkBench.Repository.SetMediaStorage(this);
        }

        public async Task<ILightAttachment> GetAsync(string resourceId, string id)
        {
            Stream stream = new MemoryStream();

            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = Container,
                Key = id
            };
            stream = await _client.GetObjectStreamAsync(Container, resourceId + "/" + id, null);

            LightAttachment _blob = new LightAttachment()
            {
                MediaStream = stream,
                Id = id,
                ResourceId = resourceId,
                ContentType = MimeMapping.MimeUtility.GetMimeMapping(id),
                Name = id
            };
            return _blob;
        }

        public async void InsertUpdateAsync(ILightAttachment attachment)
        {
            await _fileTransferUtility.UploadAsync(attachment.MediaStream, Container, attachment.ResourceId + "/" + attachment.Id);
        }

        public void Remove(ILightAttachment attachment)
        {
            DeleteObjectRequest deleteObject = new DeleteObjectRequest
            {
                BucketName = Container,
                Key = attachment.ResourceId + "/" + attachment.Id
            };
            _client.DeleteObjectAsync(deleteObject);
        }

        /// <summary>
        /// Method to run the Health Check for S3 Media Storage
        /// </summary>
        /// <param name="serviceKey"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LightHealth.HealthCheck HealthCheck(string serviceKey, string value)
        {
            try
            {
                _client.ListBucketsAsync();
                return LightHealth.HealthCheck.Healthy;
            }
            catch
            {
                return LightHealth.HealthCheck.Unhealthy;
            }                
        }
    }
}
