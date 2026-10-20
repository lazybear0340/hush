using System.CommandLine;

var root = new RootCommand("Hush - Upload and receive files securely");

var uploadCmd = new Command("upload", "Upload a file");
var fileArg = new Argument<string>("file") { Description = "The file to upload" };
var keyOpt = new Option<string?>("--key") { Description = "Encryption key (passphrase)" };
var keyEnvOpt = new Option<string?>("--key-env") { Description = "Environment variable containing the encryption key" };
var urlOpt = new Option<string?>("--url") { Description = "Custom upload endpoint URL" };

uploadCmd.Arguments.Add(fileArg);
uploadCmd.Options.Add(keyOpt);
uploadCmd.Options.Add(keyEnvOpt);
uploadCmd.Options.Add(urlOpt);

uploadCmd.SetAction(async (result, token) =>
{
    var file = result.GetValue(fileArg);
    var key = result.GetValue(keyOpt);
    var keyEnv = result.GetValue(keyEnvOpt);
    var url = result.GetValue(urlOpt);
    await Upload.UploadFile(file, key, keyEnv, url);
    return 0;
});

var receiveCmd = new Command("receive", "Receive a file from a URL");
var urlArg = new Argument<string>("url") { Description = "The URL to receive from" };
var receiveKeyOpt = new Option<string?>("--key") { Description = "Decryption key (passphrase)" };
var outputOpt = new Option<string?>("--output") { Description = "Output filename" };
var forceOpt = new Option<bool>("--force") { Description = "Overwrite existing file" };

receiveCmd.Arguments.Add(urlArg);
receiveCmd.Options.Add(receiveKeyOpt);
receiveCmd.Options.Add(outputOpt);
receiveCmd.Options.Add(forceOpt);

receiveCmd.SetAction(async (result, token) =>
{
    var url = result.GetValue(urlArg);
    var key = result.GetValue(receiveKeyOpt);
    var output = result.GetValue(outputOpt);
    var force = result.GetValue(forceOpt);
    await Receive.ReceiveFile(url, key, output, force);
    return 0;
});

root.Subcommands.Add(uploadCmd);
root.Subcommands.Add(receiveCmd);

return root.Parse(args).Invoke();

static async Task UploadFile(string file, string? key, string? keyEnv, string? url)
{
    await Upload.UploadFile(file, key, keyEnv, url);
}


static async Task ReceiveFile(string url, string? key, string? output, bool force)
{
    await Receive.ReceiveFile(url, key, output, force);
}
