using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public static class Constants
    {
        public static readonly int FP_SIZE_COM = (1404 + 12);
        public static readonly int ENROLL_DATA_SIZE = (4 * 8 + FP_SIZE_COM * 2);
        public static readonly int PHOTO_SIZE = 8192;
        public static readonly int USER_INFO_SIZE = 20;

        public static readonly int GLOG_BY_ID = 0x1;
        public static readonly int GLOG_BY_CD = 0x2;
        public static readonly int GLOG_BY_FP = 0x4;
        public static readonly int GLOG_BY_DURESS_BIT = 0x8;
        public static readonly int GLOG_BY_LIMITTIME = 0x10;
        public static readonly int GLOG_BY_ANTIPASS = 0x20;
        public static readonly int GLOG_BY_TIMEZONE = 0x40;
        public static readonly int GLOG_BY_AREA = 0x80;

        public static readonly int COMPANY_NAME_FONT_BOLD = 2;
        public static readonly int COMPANY_NAME_FONT_ITALIC = 1;
        public static readonly int DB_HOLIDAY_SIZE = 16;
        public static readonly int DB_HOLIDAY_MAX = 256;

        public static readonly int DB_TZONE_SIZE = 24;
        public static readonly int DB_TZONE_MAX = 2048;

        public const int DMASK_HOL = 0x1;
        public const int DMASK_SUN = 0x2;
        public const int DMASK_MON = 0x4;
        public const int DMASK_TUE = 0x8;
        public const int DMASK_WED = 0x10;
        public const int DMASK_THU = 0x20;
        public const int DMASK_FRI = 0x40;
        public const int DMASK_SAT = 0x80;
        public const int DMASK_ALL = 0xFF;

        public static readonly int DB_DAYLIGHT_SIZE = 24;
        public static readonly int DB_DAYLIGHT_MAX = 4;

        public static readonly int DB_TMODE_SIZE = 28;
        public static readonly int DB_TMODE_MAX = 10;

        public static readonly int DB_BELLTIME_SIZE = 20;
        public static readonly int DB_BELLTIME_MAX = 20;

        public static readonly int DB_AUTODOOR_SIZE = 24;
        public static readonly int DB_AUTODOOR_MAX = 6;

        public static readonly int DB_AUTOKEY_SIZE = 28;
        public static readonly int DB_AUTOKEY_MAX = 5;

        public static readonly int DB_NOACTKEY_SIZE = 16;

        // VoIP Related declares
        public const int VS_NONE = 0;
        public const int VS_CLOSED = 1;
        public const int VS_RINGING = 2;
        public const int VS_OPENED = 3;
        public const int VOIP_IMAGE_WIDTH = 320;
        public const int VOIP_IMAGE_HEIGHT = 240;

        public const int PHONE_COUNT = 100;
    }

    public class NoActKey
    {
        public int FKey1;
        public int FKey2;
        public int FKey3;
        public int FKey4;

        public void ReadFromBinary(byte[] data)
        {
            if (data == null || data.Length < Constants.DB_NOACTKEY_SIZE)
                return;

            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    FKey1 = reader.ReadInt32();
                    FKey2 = reader.ReadInt32();
                    FKey3 = reader.ReadInt32();
                    FKey4 = reader.ReadInt32();
                }
            }
        }

        public byte[] Serialize()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(ms);
            writer.Write(FKey1);
            writer.Write(FKey2);
            writer.Write(FKey3);
            writer.Write(FKey4);

            return ms.ToArray();
        }
    }

    public class AutoKey : DayMaskSetting
    {
        public int Valid;      // 0-Invalid, 1-Valid
        public int SHour;      // 0-23-Start Hour
        public int SMin;      // 0-59-Start Minute
        public int EHour;      // 0-23-End Hour
        public int EMin;      // 0-59-End Minute
        public int FKey;      // 0-39-Function Key

        public static AutoKey GetFromStream(Stream stream)
        {
            AutoKey ret = new AutoKey();
            BinaryReader reader = new BinaryReader(stream);

            ret.Valid = reader.ReadInt32();
            ret.DMask = reader.ReadInt32();
            ret.SHour = reader.ReadInt32();
            ret.SMin = reader.ReadInt32();
            ret.EHour = reader.ReadInt32();
            ret.EMin = reader.ReadInt32();
            ret.FKey = reader.ReadInt32();

            return ret;
        }

        public void Serialize(Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(Valid);
            writer.Write(DMask);
            writer.Write(SHour);
            writer.Write(SMin);
            writer.Write(EHour);
            writer.Write(EMin);
            writer.Write(FKey);
        }
    }

    public class AutoDoor : DayMaskSetting
    {
        public int Valid;       // 0-Invalid, 1-Valid
        public int SHour;       // 0-23-Start Hour
        public int SMin;       // 0-59-Start Minute
        public int EHour;       // 0-23-End Hour
        public int EMin;       // 0-59-End Minute

        public static AutoDoor GetFromStream(Stream stream)
        {
            AutoDoor ret = new AutoDoor();
            BinaryReader reader = new BinaryReader(stream);
            ret.Valid = reader.ReadInt32();
            ret.DMask = reader.ReadInt32();
            ret.SHour = reader.ReadInt32();
            ret.SMin = reader.ReadInt32();
            ret.EHour = reader.ReadInt32();
            ret.EMin = reader.ReadInt32();

            return ret;
        }

        public void Serialize(Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(Valid);
            writer.Write(DMask);
            writer.Write(SHour);
            writer.Write(SMin);
            writer.Write(EHour);
            writer.Write(EMin);
        }
    }

    public class BellTime : DayMaskSetting
    {
        public int Valid;       //0-Invalid, 1-Valid
        public int SHour;       //0-23-Start Hour
        public int SMin;       //0-59-Start Minute
        public int Sec;       //1-255-Interval

        public static BellTime GetFromStream(Stream stream)
        {
            BellTime ret = new BellTime();
            BinaryReader reader = new BinaryReader(stream);
            ret.Valid = reader.ReadInt32();
            ret.DMask = reader.ReadInt32();
            ret.SHour = reader.ReadInt32();
            ret.SMin = reader.ReadInt32();
            ret.Sec = reader.ReadInt32();

            return ret;
        }

        public void Serialize(Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(Valid);
            writer.Write(DMask);
            writer.Write(SHour);
            writer.Write(SMin);
            writer.Write(Sec);
        }
    }

    public class TMode : DayMaskSetting
    {
        public int Valid;       //0-Invalid, 1-Valid
        public int SHour;       //0-23-Start Hour
        public int SMin;       //0-59-Start Minute
        public int EHour;       //0-23-End Hour
        public int EMin;       //0-59-End Minute
        public int Mode;       //0-14-Door Mode

        public static TMode GetFromStream(Stream stream)
        {
            TMode ret = new TMode();
            BinaryReader reader = new BinaryReader(stream);
            ret.Valid = reader.ReadInt32();
            ret.DMask = reader.ReadInt32();
            ret.SHour = reader.ReadInt32();
            ret.SMin = reader.ReadInt32();
            ret.EHour = reader.ReadInt32();
            ret.EMin = reader.ReadInt32();
            ret.Mode = reader.ReadInt32();

            return ret;
        }

        public void Serialize(Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(Valid);
            writer.Write(DMask);
            writer.Write(SHour);
            writer.Write(SMin);
            writer.Write(EHour);
            writer.Write(EMin);
            writer.Write(Mode);
        }
    }

    public class Daylight
    {
        public int Valid;       //0-Invalid, 1-Valid
        public int Year;       //0-99
        public int Month;       //1-12
        public int Day;       //1-31
        public int Hour;       //0-23
        public int Min;       //0-59

        public static Daylight GetFromStream(Stream stream)
        {
            Daylight ret = new Daylight();
            BinaryReader reader = new BinaryReader(stream);
            {
                ret.Valid = reader.ReadInt32();
                ret.Year = reader.ReadInt32();
                ret.Month = reader.ReadInt32();
                ret.Day = reader.ReadInt32();
                ret.Hour = reader.ReadInt32();
                ret.Min = reader.ReadInt32();
            }

            return ret;
        }

        public void Serialize(Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            {
                writer.Write(Valid);
                writer.Write(Year);
                writer.Write(Month);
                writer.Write(Day);
                writer.Write(Hour);
                writer.Write(Min);
            }
        }
    }

    public class DayMaskSetting
    {
        public int DMask;
    }

    public class UiDayMaskSetting
    {
        DayMaskSetting setting;

        public UiDayMaskSetting(DayMaskSetting setting)
        {
            this.setting = setting;
        }

        public bool Holiday
        {
            get { return (setting.DMask & Constants.DMASK_HOL) == Constants.DMASK_HOL; }
            set
            {
                if (value)
                    setting.DMask |= Constants.DMASK_HOL;
                else
                    setting.DMask &= (~Constants.DMASK_HOL);
            }
        }

        public bool Monday
        {
            get { return (setting.DMask & Constants.DMASK_MON) == Constants.DMASK_MON; }
            set
            {
                if (value)
                    setting.DMask |= Constants.DMASK_MON;
                else
                    setting.DMask &= (~Constants.DMASK_MON);
            }
        }

        public bool TuesDay
        {
            get { return (setting.DMask & Constants.DMASK_TUE) == Constants.DMASK_TUE; }
            set
            {
                if (value)
                    setting.DMask |= Constants.DMASK_TUE;
                else
                    setting.DMask &= (~Constants.DMASK_TUE);
            }
        }

        public bool WendsDay
        {
            get { return (setting.DMask & Constants.DMASK_WED) == Constants.DMASK_WED; }
            set
            {
                if (value)
                    setting.DMask |= Constants.DMASK_WED;
                else
                    setting.DMask &= (~Constants.DMASK_WED);
            }
        }

        public bool ThursDay
        {
            get { return (setting.DMask & Constants.DMASK_THU) == Constants.DMASK_THU; }
            set
            {
                if (value)
                    setting.DMask |= Constants.DMASK_THU;
                else
                    setting.DMask &= (~Constants.DMASK_THU);
            }
        }

        public bool Friday
        {
            get { return (setting.DMask & Constants.DMASK_FRI) == Constants.DMASK_FRI; }
            set
            {
                if (value)
                    setting.DMask |= Constants.DMASK_FRI;
                else
                    setting.DMask &= (~Constants.DMASK_FRI);
            }
        }

        public bool Saturday
        {
            get { return (setting.DMask & Constants.DMASK_SAT) == Constants.DMASK_SAT; }
            set
            {
                if (value)
                    setting.DMask |= Constants.DMASK_SAT;
                else
                    setting.DMask &= (~Constants.DMASK_SAT);
            }
        }

        public bool Sunday
        {
            get { return (setting.DMask & Constants.DMASK_SUN) == Constants.DMASK_SUN; }
            set
            {
                if (value)
                    setting.DMask |= Constants.DMASK_SUN;
                else
                    setting.DMask &= (~Constants.DMASK_SUN);
            }
        }
    }

    public class TZone : DayMaskSetting
    {
        public int Valid;      //0-Invalid, 1-Valid
        public int SHour;      //0-23-Start Hour
        public int SMin;       //0-59-Start Minute
        public int EHour;      //0-23-End Hour
        public int EMin;       //0-59-End Minute

        public static TZone GetFromStream(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            TZone ret = new TZone();

            ret.Valid = reader.ReadInt32();
            ret.DMask = reader.ReadInt32();
            ret.SHour = reader.ReadInt32();
            ret.SMin = reader.ReadInt32();
            ret.EHour = reader.ReadInt32();
            ret.EMin = reader.ReadInt32();

            return ret;
        }

        public void Serialize(Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(Valid);
            writer.Write(DMask);
            writer.Write(SHour);
            writer.Write(SMin);
            writer.Write(EHour);
            writer.Write(EMin);
        }
    }

    public class HolidayItem
    {
        public int Valid; // '0-Invalid, 1-Valid
        public int Month; // '1-12
        public int Day; // '1-31, 1-7
        public int Number; // '0-Day is Day, 1-5-Day is Weekday

        public static HolidayItem GetFromStream(Stream stream)
        {
            HolidayItem ret = new HolidayItem();
            BinaryReader reader = new BinaryReader(stream);
            {
                ret.Valid = reader.ReadInt32();
                ret.Month = reader.ReadInt32();
                ret.Day = reader.ReadInt32();
                ret.Number = reader.ReadInt32();
            }

            return ret;
        }

        public void Serialize(Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            {
                writer.Write(Valid);
                writer.Write(Month);
                writer.Write(Day);
                writer.Write(Number);
            }
        }
    }

    public enum VerificationMode
    {
        Any = 0,                // Any
        Finger = 1,             // Finger
        CD_or_FP = 2,           // CD or FP
        IDFP_or_CD = 3,         // ID&FP or CD
        IDFP_or_IDCD = 4,       // ID&FP or ID&CD
        IDFP_or_CDFP = 5,       // ID&FP or CD&FP
        Open = 6,               // Open
        Close = 7,              // Close
        Card = 8,               // Card
        ID_or_FP = 9,           // ID or FP
        ID_or_CD = 10,          // ID or CD
        IDCD = 11,              // ID&CD
        CDFP = 12,              // CD&FP
        IDFP = 13,              // ID&FP
        IDCDFP = 14,            // ID&CD&FP
    }

    public enum FKey
    {
        F1_0,
        F1_1,
        F1_2,
        F1_3,
        F1_4,
        F1_5,
        F1_6,
        F1_7,
        F1_8,
        F1_9,
        F2_0,
        F2_1,
        F2_2,
        F2_3,
        F2_4,
        F2_5,
        F2_6,
        F2_7,
        F2_8,
        F2_9,
        F3_0,
        F3_1,
        F3_2,
        F3_3,
        F3_4,
        F3_5,
        F3_6,
        F3_7,
        F3_8,
        F3_9,
        F4_0,
        F4_1,
        F4_2,
        F4_3,
        F4_4,
        F4_5,
        F4_6,
        F4_7,
        F4_8,
        F4_9,
    }

    public static class Util
    {
        public static int pubIPAddrToLong(string txtIP)
        {
            byte[] addr = System.Net.IPAddress.Parse(txtIP).GetAddressBytes();

            return ((int)addr[0] << 24) | ((int)addr[1] << 16) | ((int)addr[2] << 8) | ((int)addr[3]);
        }

        public static string pubLongToIPAddr(long vValue)
        {
            byte[] addr = new byte[4];
            addr[0] = (byte)(vValue >> 24);
            addr[1] = (byte)(vValue >> 16);
            addr[2] = (byte)(vValue >> 8);
            addr[3] = (byte)(vValue >> 0);

            return string.Format("{0:D}.{1:D}.{2:D}.{3:D}", addr[0], addr[1], addr[2], addr[3]);
        }
    }

    public class UserInfo
    {
        public Int32 dwTzone1;
        public Int32 dwTzone2;
        public Int32 dwAccessMode;
        public Int32 dwLimitTime;

        public static UserInfo GetFromByte(byte[] data)
        {
            if (data == null || data.Length < Constants.USER_INFO_SIZE)
                return null;

            return GetFromStream(new MemoryStream(data));
        }

        public static UserInfo GetFromStream(Stream stream)
        {
            UserInfo ret = new UserInfo();
            BinaryReader reader = new BinaryReader(stream);
            {
                ret.dwTzone1 = reader.ReadInt32();
                ret.dwTzone2 = reader.ReadInt32();
                ret.dwAccessMode = reader.ReadInt32();
                ret.dwLimitTime = reader.ReadInt32();
            }

            return ret;
        }

        public byte[] Serialize()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(dwTzone1);
            writer.Write(dwTzone2);
            writer.Write(dwAccessMode);
            writer.Write(dwLimitTime);

            return stream.ToArray();
        }
    }

    public class EnrollData
    {
        public Int32 dwValidTempId;
        public Int32 dwValidCard;
        public Int32 dwValidFp1;
        public Int32 dwValidFp2;
        public Int32 dwTempIdNumber;
        public Int32 dwManager;
        public Int32[] dwCardData = new Int32[2];
        public Byte[] byFpData1 = new Byte[Constants.FP_SIZE_COM];
        public Byte[] byFpData2 = new Byte[Constants.FP_SIZE_COM];

        public static EnrollData GetFromBinary(Byte[] data)
        {
            if (data == null || data.Length < Constants.ENROLL_DATA_SIZE)
                return null;

            return GetFromStream(new MemoryStream(data));
        }

        public static EnrollData GetFromStream(MemoryStream stream)
        {
            EnrollData result = new EnrollData();
            BinaryReader reader = new BinaryReader(stream);
            result.dwValidTempId = reader.ReadInt32();
            result.dwValidCard = reader.ReadInt32();
            result.dwValidFp1 = reader.ReadInt32();
            result.dwValidFp2 = reader.ReadInt32();
            result.dwTempIdNumber = reader.ReadInt32();
            result.dwManager = reader.ReadInt32();
            result.dwCardData[0] = reader.ReadInt32();
            result.dwCardData[1] = reader.ReadInt32();

            result.byFpData1 = reader.ReadBytes(Constants.FP_SIZE_COM);
            result.byFpData2 = reader.ReadBytes(Constants.FP_SIZE_COM);

            return result;
        }

        public byte[] Serialize()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(dwValidTempId);
            writer.Write(dwValidCard);
            writer.Write(dwValidFp1);
            writer.Write(dwValidFp2);
            writer.Write(dwTempIdNumber);
            writer.Write(dwManager);
            writer.Write(dwCardData[0]);
            writer.Write(dwCardData[1]);

            writer.Write(byFpData1);
            writer.Write(byFpData2);

            return stream.ToArray();
        }
    }
}
