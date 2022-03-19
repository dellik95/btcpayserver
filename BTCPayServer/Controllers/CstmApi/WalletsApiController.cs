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
using BTCPayServer.Abstractions.Extensions;

namespace BTCPayServer.Controllers
{
    [Route("api/wallets/{walletId}/transactions")]
    [ApiController]
    [EnableCors(CorsPolicies.All)]
    public class WalletsApiController : ControllerBase
    {
        private readonly WalletRepository _walletRepository;
        private readonly LabelFactory _labelFactory;

        private WalletBlobInfo _walletBlobInfo;
        private Dictionary<string, WalletTransactionInfo> _walletTransactionsInfo;

        readonly string[] _labelColorScheme =
        {
            "#fbca04", "#0e8a16", "#ff7619", "#84b6eb", "#5319e7", "#cdcdcd", "#cc317c",
        };

        private const int MaxLabelSize = 20;
        private const int MaxCommentSize = 200;

        public WalletsApiController(WalletRepository walletRepository, LabelFactory labelFactory)
        {
            _walletRepository = walletRepository;
            _labelFactory = labelFactory;
        }


        [HttpPost]
        [Route("{id}")]
        public async Task<IActionResult> ModifyTransaction(
            [ModelBinder(typeof(WalletIdModelBinder))]
            WalletId walletId, string id, [FromBody] ModifyTransactionDto input)
        {
            if (input == null) return BadRequest("Input not valid");
            await InitWalletInfo(walletId);
            try
            {
                if (!string.IsNullOrEmpty(input.LabelToAdd))
                {
                    await AddTransactionLabel(walletId, id, input.LabelToAdd);
                }

                if (!string.IsNullOrEmpty(input.LabelToRemove))
                {
                    await RemoveTransactionLabel(walletId, id, input.LabelToRemove, _walletTransactionsInfo,
                        _walletBlobInfo);
                }

                if (!string.IsNullOrEmpty(input.Comment))
                {
                    await AddTransactionComment(walletId, id, input.Comment, _walletTransactionsInfo);
                }
            }
            catch (Exception e)
            {
                return StatusCode(500, "Internal server error");
            }

            return Ok(new {walletId, id});
        }

        private WalletTransactionInfo GetWalletTransactionInfo(string transactionId)
        {
            if (!_walletTransactionsInfo.TryGetValue(transactionId, out var walletTransactionInfo))
            {
                walletTransactionInfo = new WalletTransactionInfo();
            }

            return walletTransactionInfo;
        }

        private async Task InitWalletInfo(WalletId walletId)
        {
            _walletBlobInfo = await _walletRepository.GetWalletInfo(walletId);
            _walletTransactionsInfo = await _walletRepository.GetWalletTransactionsInfo(walletId);
        }

        private async Task AddTransactionLabel(WalletId walletId, string transactionId, string label)
        {
            label = label.Trim().TrimStart('{').ToLowerInvariant().Replace(',', ' ')
                .Truncate(MaxLabelSize);
            var labels = _labelFactory.GetWalletColoredLabels(_walletBlobInfo, Request);
            WalletTransactionInfo walletTransactionInfo = GetWalletTransactionInfo(transactionId);

            if (!labels.Any(l => l.Text.Equals(label, StringComparison.OrdinalIgnoreCase)))
            {
                await ApplyLabelColor(walletId, labels, _walletBlobInfo, label);
            }

            var rawLabel = new RawLabel(label);
            if (walletTransactionInfo.Labels.TryAdd(rawLabel.Text, (LabelData)rawLabel))
            {
                await _walletRepository.SetWalletTransactionInfo(walletId, transactionId, walletTransactionInfo);
            }
        }


        private async Task ApplyLabelColor(WalletId walletId, IEnumerable<ColoredLabel> labels,
            WalletBlobInfo walletBlobInfo, string label)
        {
            List<string> allColors = new();
            allColors.AddRange(_labelColorScheme);
            allColors.AddRange(labels.Select(l => l.Color));
            var chosenColor =
                allColors
                    .GroupBy(k => k)
                    .OrderBy(k => k.Count())
                    .ThenBy(k =>
                    {
                        var indexInColorScheme = Array.IndexOf(_labelColorScheme, k.Key);
                        return indexInColorScheme == -1 ? double.PositiveInfinity : indexInColorScheme;
                    })
                    .First().Key;
            walletBlobInfo.LabelColors.Add(label, chosenColor);
            await _walletRepository.SetWalletInfo(walletId, walletBlobInfo);
        }

        private async Task AddTransactionComment(WalletId walletId, string transactionId, string comment,
            Dictionary<string, WalletTransactionInfo> walletTransactionsInfo)
        {
            comment = comment.Trim().Truncate(MaxCommentSize);
            if (!walletTransactionsInfo.TryGetValue(transactionId, out var walletTransactionInfo))
            {
                walletTransactionInfo = new WalletTransactionInfo();
            }

            walletTransactionInfo.Comment = comment;
            await _walletRepository.SetWalletTransactionInfo(walletId, transactionId, walletTransactionInfo);
        }

        private async Task RemoveTransactionLabel(WalletId walletId, string id, string removeLabel,
            Dictionary<string, WalletTransactionInfo> walletTransactionsInfo,
            WalletBlobInfo walletBlobInfo)
        {
            removeLabel = removeLabel.Trim().ToLowerInvariant().Replace(',', ' ');
            if (_walletTransactionsInfo.TryGetValue(id, out var walletTransactionInfo))
            {
                if (walletTransactionInfo.Labels.Remove(removeLabel))
                {
                    var canDeleteColor =
                        !walletTransactionsInfo.Any(txi => txi.Value.Labels.ContainsKey(removeLabel));
                    if (canDeleteColor)
                    {
                        walletBlobInfo.LabelColors.Remove(removeLabel);
                        await _walletRepository.SetWalletInfo(walletId, walletBlobInfo);
                    }

                    await _walletRepository.SetWalletTransactionInfo(walletId, id,
                        walletTransactionInfo);
                }
            }
        }


        public class ModifyTransactionDto
        {
            public string LabelToAdd { get; set; }
            public string Comment { get; set; }
            public string LabelToRemove { get; set; }
        }
    }
}
