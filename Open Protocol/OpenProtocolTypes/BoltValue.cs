namespace OpenProtocol.OpenProtocolTypes
{
    public struct TighteningValue
    {
        public int BoltNumber;
        public int SimpleBoltStatus;
        public int TorqueStatus;
        public int AngleStatus;
        public float BoltTorque;
        public float BoltAngle;
        public float BoltTorqueHighLimit;
        public float BoltTorqueLowLimit;
        public float BoltAngleHighLimit;
        public float BoltAngleLowLimit;
    }


    public struct FitingValue
    {
        public int FittingNumber;
        public int SimpleFittingStatus;
        public int ForceStatus;
        public int StrokeStatus;
        public float FittingForce;
        public float FittingStroke;
        public float FittingForceHighLimit;
        public float FittingForceLowLimit;
        public float FittingStrokeHighLimit;
        public float FittingStrokeLowLimit;
    }
}