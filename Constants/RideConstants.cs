namespace NearU_Backend_Revised.Constants
{
    public static class RideConstants
    {
        // Faculty of Computing centroid
        public const double FacultyCentroidLat = 6.7145;
        public const double FacultyCentroidLng = 80.7872;
        
        public const int MaxOtpAttempts = 3;
        public const int OtpExpiryMinutes = 10;
        public const int CancellationWindowSeconds = 60;
        public const decimal CancellationPenalty = 20.0m;
        public const int AllowedRadiusMeters = 5000;
    }
}
