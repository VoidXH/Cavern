using Cavern.Format.Consts;
using Cavern.Utilities;

namespace Cavern.Format.Container.MXF {
    internal partial class KeyLengthValueSMPTE {
        /// <inheritdoc/>
        public override string ToString() => Key.marker == MXFConsts.universalLabelLE ?
            $"{RegistryToString()} {ItemToString()} ({Length} bytes)" :
            $"{KeyBlockToString(Key.marker)} {RegistryToString()} {ItemToString()} ({Length} bytes)";

        /// <summary>
        /// Convert part of the <see cref="Key"/> to the common written format of UL designators.
        /// </summary>
        static string KeyBlockToString(int block) {
            QMath.ConverterStruct bytes = new QMath.ConverterStruct {
                asInt = block
            };
            return $"{bytes.byte3:X2}.{bytes.byte2:X2}.{bytes.byte1:X2}.{bytes.byte0:X2}";
        }

        /// <summary>
        /// Convert the <see cref="Key"/>'s item to string if it's known.
        /// </summary>
        protected virtual string ItemToString() => Key.registry switch {
            MXFConsts.immersiveAudioRegistry => Key.item switch {
                MXFConsts.immersiveAudioEssence => "Essence",
                _ => GenericItemToString(),
            },
            _ => GenericItemToString(),
        };

        /// <summary>
        /// Convert the <see cref="Key"/>'s registry to string if it's known.
        /// </summary>
        string RegistryToString() => Key.registry switch {
            MXFConsts.packRegistry => "Pack",
            MXFConsts.immersiveAudioRegistry => "Immersive Audio",
            _ => KeyBlockToString(Key.registry),
        };

        /// <summary>
        /// Convert the <see cref="Key"/>'s item to string if it's unknown.
        /// </summary>
        string GenericItemToString() => $"{KeyBlockToString((int)(Key.item >> 32))} {KeyBlockToString((int)Key.item)}";
    }
}