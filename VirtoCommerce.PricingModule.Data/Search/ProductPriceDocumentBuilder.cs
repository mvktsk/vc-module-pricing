﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Pricing.Model;
using VirtoCommerce.Domain.Pricing.Services;
using VirtoCommerce.Domain.Search;

namespace VirtoCommerce.PricingModule.Data.Search
{
    public class ProductPriceDocumentBuilder : IIndexDocumentBuilder
    {
        private readonly IPricingService _pricingService;

        public ProductPriceDocumentBuilder(IPricingService pricingService)
        {
            _pricingService = pricingService;
        }

        public virtual Task<IList<IndexDocument>> GetDocumentsAsync(IList<string> documentIds)
        {
            var prices = GetProductPrices(documentIds);

            IList<IndexDocument> result = prices
                .GroupBy(p => p.ProductId)
                .Select(g => CreateDocument(g.Key, g.ToArray()))
                .ToArray();

            return Task.FromResult(result);
        }


        protected virtual IndexDocument CreateDocument(string productId, IList<Price> prices)
        {
            var document = new IndexDocument(productId);

            if (prices != null)
            {
                foreach (var price in prices)
                {
                    document.Add(new IndexDocumentField($"price_{price.Currency}_{price.PricelistId}".ToLowerInvariant(), price.EffectiveValue) { IsRetrievable = true, IsFilterable = true });

                    // Save additional pricing fields for convinient user searches, store price with currency and without one
                    document.Add(new IndexDocumentField($"price_{price.Currency}".ToLowerInvariant(), price.EffectiveValue) { IsRetrievable = true, IsFilterable = true, IsCollection = true });
                    document.Add(new IndexDocumentField("price", price.EffectiveValue) { IsRetrievable = true, IsFilterable = true, IsCollection = true });
                }
            }

            document.Add(new IndexDocumentField("is", prices?.Count > 0 ? "priced" : "unpriced") { IsRetrievable = true, IsFilterable = true, IsCollection = true });

            return document;
        }

        protected virtual IList<Price> GetProductPrices(IList<string> productIds)
        {
            var evalContext = new PriceEvaluationContext { ProductIds = productIds.ToArray() };
            return _pricingService.EvaluateProductPrices(evalContext).ToList();
        }
    }
}
