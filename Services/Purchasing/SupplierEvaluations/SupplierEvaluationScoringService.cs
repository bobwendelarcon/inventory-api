namespace inventory_api.Services.Purchasing.SupplierEvaluations
{
    public class SupplierEvaluationScoringService
    {
        public const decimal QualityWeight = 0.40m;
        public const decimal DeliveryWeight = 0.30m;
        public const decimal CostWeight = 0.20m;
        public const decimal ReliabilityWeight = 0.10m;

        /// <summary>
        /// Calculates the weighted Quality score.
        /// </summary>
        public decimal CalculateQualityWeightedScore(decimal qualityScore)
        {
            qualityScore = NormalizeScore(qualityScore);

            return RoundScore(qualityScore * QualityWeight);
        }

        /// <summary>
        /// Calculates the weighted Delivery score.
        /// </summary>
        public decimal CalculateDeliveryWeightedScore(decimal deliveryScore)
        {
            deliveryScore = NormalizeScore(deliveryScore);

            return RoundScore(deliveryScore * DeliveryWeight);
        }

        /// <summary>
        /// Calculates the weighted Cost Competitiveness score.
        /// </summary>
        public decimal CalculateCostWeightedScore(decimal costScore)
        {
            costScore = NormalizeScore(costScore);

            return RoundScore(costScore * CostWeight);
        }

        /// <summary>
        /// Calculates the weighted Reliability score.
        /// </summary>
        public decimal CalculateReliabilityWeightedScore(
            decimal reliabilityScore)
        {
            reliabilityScore = NormalizeScore(reliabilityScore);

            return RoundScore(reliabilityScore * ReliabilityWeight);
        }

        /// <summary>
        /// Calculates the total weighted supplier evaluation score.
        /// </summary>
        public decimal CalculateTotalScore(
            decimal qualityScore,
            decimal deliveryScore,
            decimal costScore,
            decimal reliabilityScore)
        {
            var qualityWeighted =
                CalculateQualityWeightedScore(qualityScore);

            var deliveryWeighted =
                CalculateDeliveryWeightedScore(deliveryScore);

            var costWeighted =
                CalculateCostWeightedScore(costScore);

            var reliabilityWeighted =
                CalculateReliabilityWeightedScore(reliabilityScore);

            return RoundScore(
                qualityWeighted +
                deliveryWeighted +
                costWeighted +
                reliabilityWeighted);
        }

        /// <summary>
        /// Calculates the Reliability score from its four manual criteria.
        /// </summary>
        public decimal CalculateReliabilityScore(
            decimal responsivenessScore,
            decimal issueResolutionScore,
            decimal replacementSupportScore,
            decimal communicationScore)
        {
            responsivenessScore =
                NormalizeScore(responsivenessScore);

            issueResolutionScore =
                NormalizeScore(issueResolutionScore);

            replacementSupportScore =
                NormalizeScore(replacementSupportScore);

            communicationScore =
                NormalizeScore(communicationScore);

            var score =
                (
                    responsivenessScore +
                    issueResolutionScore +
                    replacementSupportScore +
                    communicationScore
                ) / 4m;

            return RoundScore(score);
        }

        /// <summary>
        /// Returns the performance rating for the total score.
        /// </summary>
        public string GetPerformanceRating(decimal totalScore)
        {
            totalScore = NormalizeScore(totalScore);

            return totalScore switch
            {
                >= 90m => "EXCELLENT",
                >= 80m => "VERY_GOOD",
                >= 70m => "GOOD",
                >= 60m => "NEEDS_IMPROVEMENT",
                _ => "POOR"
            };
        }

        /// <summary>
        /// Calculates all raw, weighted and total scores.
        /// </summary>
        public SupplierEvaluationScoreResult CalculateAllScores(
            decimal qualityScore,
            decimal deliveryScore,
            decimal costScore,
            decimal reliabilityScore)
        {
            qualityScore = NormalizeScore(qualityScore);
            deliveryScore = NormalizeScore(deliveryScore);
            costScore = NormalizeScore(costScore);
            reliabilityScore = NormalizeScore(reliabilityScore);

            var qualityWeightedScore =
                CalculateQualityWeightedScore(qualityScore);

            var deliveryWeightedScore =
                CalculateDeliveryWeightedScore(deliveryScore);

            var costWeightedScore =
                CalculateCostWeightedScore(costScore);

            var reliabilityWeightedScore =
                CalculateReliabilityWeightedScore(reliabilityScore);

            var totalScore = RoundScore(
                qualityWeightedScore +
                deliveryWeightedScore +
                costWeightedScore +
                reliabilityWeightedScore);

            return new SupplierEvaluationScoreResult
            {
                QualityScore = qualityScore,
                QualityWeightedScore = qualityWeightedScore,

                DeliveryScore = deliveryScore,
                DeliveryWeightedScore = deliveryWeightedScore,

                CostScore = costScore,
                CostWeightedScore = costWeightedScore,

                ReliabilityScore = reliabilityScore,
                ReliabilityWeightedScore = reliabilityWeightedScore,

                TotalScore = totalScore,
                PerformanceRating = GetPerformanceRating(totalScore)
            };
        }

        /// <summary>
        /// Ensures that a score stays between 0 and 100.
        /// </summary>
        private static decimal NormalizeScore(decimal score)
        {
            if (score < 0m)
            {
                return 0m;
            }

            if (score > 100m)
            {
                return 100m;
            }

            return score;
        }

        private static decimal RoundScore(decimal score)
        {
            return Math.Round(
                score,
                2,
                MidpointRounding.AwayFromZero);
        }
    }

    public class SupplierEvaluationScoreResult
    {
        public decimal QualityScore { get; set; }

        public decimal QualityWeightedScore { get; set; }

        public decimal DeliveryScore { get; set; }

        public decimal DeliveryWeightedScore { get; set; }

        public decimal CostScore { get; set; }

        public decimal CostWeightedScore { get; set; }

        public decimal ReliabilityScore { get; set; }

        public decimal ReliabilityWeightedScore { get; set; }

        public decimal TotalScore { get; set; }

        public string PerformanceRating { get; set; } = string.Empty;
    }
}