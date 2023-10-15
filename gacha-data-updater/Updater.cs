using MementoMori;
using MementoMori.Extensions;
using MementoMori.Ortega.Share.Data.ApiInterface.Auth;
using MementoMori.Ortega.Share.Data.ApiInterface.Gacha;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace updater;

public class Updater(
        MementoNetworkManager networkManager, 
        IOptions<AuthOption> authOption, 
        IOptions<UpdaterOption> updaterOption, 
        ILogger<Updater> logger)
    : BackgroundService
{
    private readonly AuthOption _authOption = authOption.Value;
    private readonly UpdaterOption _updaterOption = updaterOption.Value;

    private const string GachaListFileName = "gacha_list.json";
    private const string GachaRatesFileName = "gacha_rates_{0}.json";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Logging in...");
        // login
        var reqBody = new LoginRequest()
        {
            ClientKey = _authOption.ClientKey,
            DeviceToken = _authOption.DeviceToken,
            AppVersion = networkManager.AppAssetVersionInfo.Version,
            OSVersion = _authOption.OSVersion,
            ModelName = _authOption.ModelName,
            AdverisementId = Guid.NewGuid().ToString("D"),
            UserId = _authOption.UserId
        };
        var playerDataInfoList = await networkManager.GetPlayerDataInfoList(reqBody);
        var worldId = playerDataInfoList.OrderByDescending(d => d.LastLoginTime).First().WorldId;
        await networkManager.Login(worldId);

        // mkdir
        logger.LogInformation("Creating directory...");
        Directory.CreateDirectory(_updaterOption.TargetPath);

        // get gacha list
        logger.LogInformation("Getting gacha list...");
        var getListResponse = await networkManager.GetResponse<GetListRequest, GetListResponse>(new GetListRequest());
        var gachaListJson = getListResponse.GachaCaseInfoList.ToJson(true);
        await File.WriteAllTextAsync(Path.Combine(_updaterOption.TargetPath, GachaListFileName), gachaListJson);

        // get rates
        foreach (var gachaCaseInfo in getListResponse.GachaCaseInfoList)
        {
            foreach (var gachaButtonInfo in gachaCaseInfo.GachaButtonInfoList)
            {
                logger.LogInformation("Getting gacha rates for {0}...", gachaButtonInfo.GachaButtonId);
                var lotteryItemListResponse = await networkManager.GetResponse<GetLotteryItemListRequest, GetLotteryItemListResponse>(new()
                {
                    GachaButtonId = gachaButtonInfo.GachaButtonId
                });
                var gachaRatesJson = lotteryItemListResponse.ToJson(true);
                await File.WriteAllTextAsync(Path.Combine(_updaterOption.TargetPath, string.Format(GachaRatesFileName, gachaButtonInfo.GachaButtonId)), gachaRatesJson);
            }
        }

        logger.LogInformation("Done.");
    }
}