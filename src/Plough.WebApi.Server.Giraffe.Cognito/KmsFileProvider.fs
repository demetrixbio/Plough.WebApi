namespace Plough.WebApi.Server.Giraffe.Cognito

[<CLIMutable>]
type KmsKey =
    { Region : string
      KeyId : string }

[<AutoOpen>]
module KmsFileProvider =
    open System.IO
    open Amazon
    open Amazon.KeyManagementService
    open Amazon.KeyManagementService.Model
    open Microsoft.Extensions.FileProviders
    
    type IFileProvider with
        member fileProvider.AddAwsKmsDecryption(config: KmsKey) = 
            {
                new IFileProvider with
                    member __.GetDirectoryContents subpath = fileProvider.GetDirectoryContents subpath
                    member __.Watch filter = fileProvider.Watch filter

                    member __.GetFileInfo subpath = 
                        let x = fileProvider.GetFileInfo subpath
                        {
                            new IFileInfo with 
                                member __.Exists = x.Exists
                                member __.IsDirectory = x.IsDirectory
                                member __.LastModified = x.LastModified
                                member __.Length = x.Length
                                member __.Name = x.Name
                                member __.PhysicalPath = 
                                    // as of net5, createreadstream won't get used if this is set
                                    null 
                                member __.CreateReadStream() = 
                                    let cfg = AmazonKeyManagementServiceConfig ()
                                    cfg.RegionEndpoint <- RegionEndpoint.GetBySystemName(config.Region)
                                    use kmsClient = new AmazonKeyManagementServiceClient(cfg)
                                    
                                    use source = x.CreateReadStream()
                                    use memory = new MemoryStream(capacity = int source.Length)
                                    source.CopyTo(memory)
                                    let decryptRequest = DecryptRequest (CiphertextBlob = memory, KeyId = config.KeyId)
                                    let plainText = kmsClient.DecryptAsync(decryptRequest).Result.Plaintext;
                                    upcast plainText
                        }
            }