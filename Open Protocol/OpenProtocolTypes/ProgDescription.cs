namespace OpenProtocol.OpenProtocolTypes
{
    public class ProgDescription
    {
        public int Id { get; private set; }
        public string Name { get; private set; }

        public int Rotation_Direction { get; private set; }
        public int Torque_Min { get; private set; }
        public int Torque_Max { get; private set; }
        public int Torque_Target { get; private set; }
        public int Angle_Min { get; private set; }
        public int Angle_Max { get; private set; }
        public int Angle_Target { get; private set; }



        public ProgDescription(string RawData)
        {
            Id = int.Parse(RawData.Substring(22, 3).Trim());
            Name = RawData.Substring(27, 25).Trim();
            Rotation_Direction = int.Parse(RawData.Substring(54, 2).Trim());
            Torque_Min = int.Parse(RawData.Substring(57, 3).Trim());
            Torque_Max = int.Parse(RawData.Substring(61, 6).Trim());
            Torque_Target = int.Parse(RawData.Substring(75, 6).Trim());
            Angle_Min = int.Parse(RawData.Substring(85, 5).Trim());
            Angle_Max = int.Parse(RawData.Substring(92, 5).Trim());
            Angle_Target = int.Parse(RawData.Substring(99, 5).Trim());

        }
    }
}
