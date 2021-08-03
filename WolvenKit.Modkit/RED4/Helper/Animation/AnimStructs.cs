using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using WolvenKit.Common;
using WolvenKit.Common.Extensions;
using WolvenKit.Common.FNV1A;
using WolvenKit.Common.Oodle;
using WolvenKit.Common.Services;
using WolvenKit.Modkit.RED4.GeneralStructs;
using WolvenKit.RED4.CR2W;
using WolvenKit.RED4.CR2W.Archive;
using WolvenKit.RED4.CR2W.Types;

namespace WolvenKit.Modkit.RED4.Animation
{
    #region Declarations
    using Quat = System.Numerics.Quaternion;
    using Vec4 = System.Numerics.Vector4;
    using Vec3 = System.Numerics.Vector3;
    #endregion
    #region Enums
    public enum animQualityPreset
    {
        HIGH = 0,
        MID = 16,
        LOW = 12,
        CINEMATIC_HIGH = HIGH
    }
    public enum AnimEventType
    {
        Effect = 0,
        EffectDuration = 1,
        FoleyAction = 2,
        FootIK = 3,
        FootPhase = 4,
        FootPlant = 5,
        SceneItem = 6,
        Valued = 7
    }
    public enum AnimKeyType
    {
        POSITION = 0,
        ROTATION = 2,
        SCALE = 4,
        ROTATION_INVERTED = 10, /// Unsure on 10 - its bones that are linked to tracks/IK/attachments - heels, eyes, index/middle finger
        TRANSFORM_V4Q4 = 11,
        TRANSFORM_V3Q4 = 12,
        POSITION_V4 = 13,
        TRANSLATION_V3 = 14,
    }
    public enum MotionCompression
    {
        UNCOMPRESSED = 0,
        SPLINE_MID = 2,
        UNCOMPRESSED_ALL_ANGLES = 3,
        SPLINE_LOW = 4,
        SPLINE_HIGH = 5,
        LINEAR = 6,
        UNCOMPRESSED_2D = 7,
        UNCOMPRESSED_3D_FALLBACKING = 8,
        UNCOMPRESSED_ALL_ANGLES_FALLBACKING = 9
    }
    public enum Facial_Part
    {
        NONE = 0,
        FACE = 1,
        TONGUE = 2,
        EYES = 3
    }
    public enum Facial_Region
    {
        FACE_REGION_EYES = 0,
        FACE_REGION_NOSE = 1,
        FACE_REGION_MOUTH = 2,
        FACE_REGION_JAW = 3,
        FACE_REGION_EARS = 4,
        FACE_REGION_NONE = 255
    }
    public enum Facial_RegionUpLo
    {
        NONE = 0, // ??
        UPPER_FACE = 1,
        LOWER_FACE = 2,
    }
    public enum Facial_RegionSides
    {
        NONE = 0,
        LEFT = 1,
        RIGHT = 2,
    }
    #endregion
    #region Transforms
    public class animKeyRaw
    {
        public int bone { get; set; }
        public string boneName { get; set; }
        public AnimKeyType mode { get; set; }
        public float time { get; set; }
        public int frame { get; set; }
        public Quat rot { get; set; }
        public Vec3 pos { get; set; }
        public Vec3 scale { get; set; }
        public int key { get; set; }
        public bool wSignBit { get; set; }

        public animKeyRaw()
        {
        }
        public animKeyRaw(BinaryReader br, float duration)
        {
            time = br.readDeltaTime(duration);
            frame = (int)Math.Round(time * 30, 0);
            var bf = (int)br.ReadUInt16();
            bone = bf & 0x0FFF;
            mode = (AnimKeyType)(bf >> 12);
            wSignBit = ((bf >> 12) & 8) == 8;
            switch (mode)
            {
                case AnimKeyType.SCALE:
                    scale = br.readVec3();
                    break;
                case AnimKeyType.POSITION:
                    pos = br.readVec3();
                    break;
                case AnimKeyType.ROTATION_INVERTED:
                case AnimKeyType.ROTATION:
                    rot = br.readQuatXYZ(wSignBit);
                    break;
            }
        }
        public animKeyRaw(BinaryReader br, bool isConst = false)
        {
            var bf = br.ReadUInt16();
            bone = bf & 0x0FFF;
            mode = (AnimKeyType)(bf >> 12);
            key = (int)br.ReadUInt16();
            frame = 0;
            time = 0.0f;
            wSignBit = ((bf >> 12) & 8) == 8;
            switch (mode)
            {
                case AnimKeyType.SCALE:
                    scale = br.readVec3();
                    break;
                case AnimKeyType.POSITION:
                    pos = br.readVec3();
                    break;
                case AnimKeyType.ROTATION_INVERTED:
                case AnimKeyType.ROTATION:
                    rot = br.readQuatXYZ(wSignBit);
                    break;
            }
        }
        public animKeyRaw(BinaryReader br, float duration, bool HasRawRotations = false)
        {
            time = br.readDeltaTime(duration);
            frame = (int)Math.Round(time * 30, 0);
            var bf = br.ReadUInt16();
            bone = bf & 0x0FFF;
            mode = (AnimKeyType)(bf >> 12);
            wSignBit = ((bf >> 12) & 8) == 8;
            switch (mode)
            {
                case AnimKeyType.SCALE:
                    scale = br.readVec3();
                    break;
                case AnimKeyType.POSITION:
                    pos = br.readVec3();
                    break;
                case AnimKeyType.ROTATION_INVERTED:
                case AnimKeyType.ROTATION:
                    if (HasRawRotations)
                    {
                        rot = br.readQuatXYZ(wSignBit);
                    }
                    else
                    {
                        rot = br.readQuat48(wSignBit);
                    }
                    break;
            }
        }
        /*public override string ToString()
        {
            return "[" + bone + "] " + boneName + " @" + time.ToString() + mode.ToString();
        }*/
    }
    public class animKey_Track
    {
        public int idx { get; set; }
        public string name { get; set; }
        public int key { get; set; }
        public float time { get; set; }
        public int frame { get; set; }
        public float value { get; set; }
        public int flag { get; set; }
        public bool isconst { get; set; }
        public animKey_Track()
        {

        }
        public animKey_Track(BinaryReader br, float duration, bool isConstant = false)
        {
            isconst = isConstant;
            if (!isConstant)
            {
                time = br.readDeltaTime(duration);
                frame = (int)Math.Round(time * 30, 0);
                var bf = br.ReadUInt16();
                idx = bf & 0x0FFF;
                flag = bf >> 12;
                value = br.ReadSingle();
            }
            else
            {
                frame = 0;
                time = 0.0f;
                idx = br.ReadUInt16();
                key = br.ReadUInt16();
                value = br.ReadSingle();

            }
        }
    }
    public class animTransformKey
    {
        public int bone { get; set; }
        public float time { get; set; }
        public int frame { get; set; }
        public AnimKeyType mode { get; set; }
        public Vec4 position { get; set; }
        public Quat orientation { get; set; }
        public Vec3 translation { get; set; }
    }
    #endregion

    class AnimStructs
    {
    }
}
