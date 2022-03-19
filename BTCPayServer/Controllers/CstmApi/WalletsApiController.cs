using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BTCPayServer.Client.Models;
using BTCPayServer.Data;
using BTCPayServer.ModelBinders;
using BTCPayServer.Services;
using BTCPayServer.Services.Labels;
using BTCPayServer.Services.Wallets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using BTCPayServer.Services.Stores;

namespace BTCPayServer.Controllers
{
    [Route("api/wallets")]
    [ApiController]
    [EnableCors(CorsPolicies.All)]
    public class WalletsApiController : ControllerBase
    {
        private readonly WalletRepository _walletRepository;
        private readonly BTCPayWalletProvider _walletProvider;
        private readonly LabelFactory _labelFactory;
        private readonly BTCPayNetworkProvider _networkProvider;
        private readonly StoreRepository _storeRepository;
        

        private WalletBlobInfo _walletBlobInfo;
        private Dictionary<string, WalletTransactionInfo> _walletTransactionsInfo;


        public WalletsApiController(WalletRepository walletRepository,
            BTCPayWalletProvider walletProvider,
            LabelFactory labelFactory, BTCPayNetworkProvider networkProvider,
            StoreRepository storeRepository
            )
        {
            _walletRepository = walletRepository;
            _walletProvider = walletProvider;
            _labelFactory = labelFactory;
            _networkProvider = networkProvider;
            _storeRepository = storeRepository;
            
        }


        [HttpPost]
        [Route("{walletId}")]
        public async Task<IActionResult> ModifyTransaction(
            [ModelBinder(typeof(WalletIdModelBinder))]
            WalletId walletId, [FromBody] ModifyTransactionDto input)
        {
            await InitWalletInfo(walletId);

            if (!string.IsNullOrEmpty(input.LabelToAdd))
            {
                await AddTransactionLabel(walletId, input.TransactionId, input.LabelToAdd);
            }

            if (!string.IsNullOrEmpty(input.LableToRemove))
            {
                await RemoveTransactionLabel(walletId, input, _walletTransactionsInfo, _walletBlobInfo);
            }

            if (!string.IsNullOrEmpty(input.Comment))
            {
                await AddTransactionComment(walletId, input, _walletTransactionsInfo);
            }

            return Ok(new
            {
                walletId = walletId.ToString(),
                transactionId = input.TransactionId,
                addLabel = input.LabelToAdd,
                addcomment = input.Comment,
                removelabel = input.LableToRemove
            });
        }


        private async Task InitWalletInfo(WalletId walletId)
        {
            _walletBlobInfo = await _walletRepository.GetWalletInfo(walletId);
            _walletTransactionsInfo = await _walletRepository.GetWalletTransactionsInfo(walletId);
        }

        private async Task AddTransactionLabel(WalletId walletId, string transactionId, string label)
        {

        }

        

        private async Task AddTransactionComment(WalletId walletId, ModifyTransactionDto input,
            Dictionary<string, WalletTransactionInfo> walletTransactionsInfo)
        {
            
        }

        private async Task RemoveTransactionLabel(WalletId walletId, ModifyTransactionDto input,
            Dictionary<string, WalletTransactionInfo> walletTransactionsInfo,
            WalletBlobInfo walletBlobInfo)
        {
            
        }


        public class ModifyTransactionDto
        {
            public string TransactionId { get; set; }
            public string LabelToAdd { get; set; }
            public string Comment { get; set; }
            public string LableToRemove { get; set; }

        }
    }
}
