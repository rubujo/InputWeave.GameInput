using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput
{
    /// <summary>
    /// 建立 GameInput force feedback 與 rumble 參數的 managed helper。
    /// </summary>
    public static class GameInputForceFeedback
    {
        public static GameInputRumbleParams Rumble(float lowFrequency, float highFrequency, float leftTrigger = 0, float rightTrigger = 0)
        {
            return new GameInputRumbleParams
            {
                LowFrequency = lowFrequency,
                HighFrequency = highFrequency,
                LeftTrigger = leftTrigger,
                RightTrigger = rightTrigger
            };
        }

        public static GameInputForceFeedbackEnvelope Envelope(
            ulong attackDuration = 0,
            ulong sustainDuration = 0,
            ulong releaseDuration = 0,
            float attackGain = 1,
            float sustainGain = 1,
            float releaseGain = 1,
            uint playCount = 1,
            ulong repeatDelay = 0)
        {
            return new GameInputForceFeedbackEnvelope
            {
                AttackDuration = attackDuration,
                SustainDuration = sustainDuration,
                ReleaseDuration = releaseDuration,
                AttackGain = attackGain,
                SustainGain = sustainGain,
                ReleaseGain = releaseGain,
                PlayCount = playCount,
                RepeatDelay = repeatDelay
            };
        }

        public static GameInputForceFeedbackMagnitude Magnitude(
            float linearX = 0,
            float linearY = 0,
            float linearZ = 0,
            float angularX = 0,
            float angularY = 0,
            float angularZ = 0,
            float normal = 0)
        {
            return new GameInputForceFeedbackMagnitude
            {
                LinearX = linearX,
                LinearY = linearY,
                LinearZ = linearZ,
                AngularX = angularX,
                AngularY = angularY,
                AngularZ = angularZ,
                Normal = normal
            };
        }

        public static GameInputForceFeedbackParams Constant(GameInputForceFeedbackMagnitude magnitude, GameInputForceFeedbackEnvelope envelope)
        {
            return new GameInputForceFeedbackParams
            {
                Kind = GameInputForceFeedbackEffectKind.GameInputForceFeedbackConstant,
                Constant = new GameInputForceFeedbackConstantParams
                {
                    Magnitude = magnitude,
                    Envelope = envelope
                }
            };
        }

        public static GameInputForceFeedbackParams Ramp(GameInputForceFeedbackMagnitude startMagnitude, GameInputForceFeedbackMagnitude endMagnitude, GameInputForceFeedbackEnvelope envelope)
        {
            return new GameInputForceFeedbackParams
            {
                Kind = GameInputForceFeedbackEffectKind.GameInputForceFeedbackRamp,
                Ramp = new GameInputForceFeedbackRampParams
                {
                    StartMagnitude = startMagnitude,
                    EndMagnitude = endMagnitude,
                    Envelope = envelope
                }
            };
        }

        public static GameInputForceFeedbackParams Periodic(
            GameInputForceFeedbackEffectKind kind,
            GameInputForceFeedbackMagnitude magnitude,
            GameInputForceFeedbackEnvelope envelope,
            float frequency,
            float phase = 0,
            float bias = 0)
        {
            GameInputForceFeedbackPeriodicParams parameters = new()
            {
                Magnitude = magnitude,
                Envelope = envelope,
                Frequency = frequency,
                Phase = phase,
                Bias = bias
            };

            GameInputForceFeedbackParams result = new()
            {
                Kind = kind
            };

            switch (kind)
            {
                case GameInputForceFeedbackEffectKind.GameInputForceFeedbackSineWave:
                    result.SineWave = parameters;
                    break;
                case GameInputForceFeedbackEffectKind.GameInputForceFeedbackSquareWave:
                    result.SquareWave = parameters;
                    break;
                case GameInputForceFeedbackEffectKind.GameInputForceFeedbackTriangleWave:
                    result.TriangleWave = parameters;
                    break;
                case GameInputForceFeedbackEffectKind.GameInputForceFeedbackSawtoothUpWave:
                    result.SawtoothUpWave = parameters;
                    break;
                case GameInputForceFeedbackEffectKind.GameInputForceFeedbackSawtoothDownWave:
                    result.SawtoothDownWave = parameters;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(kind), kind, "必須指定 periodic force feedback 類型。");
            }

            return result;
        }

        public static GameInputForceFeedbackParams Condition(GameInputForceFeedbackEffectKind kind, GameInputForceFeedbackConditionParams condition)
        {
            GameInputForceFeedbackParams result = new()
            {
                Kind = kind
            };

            switch (kind)
            {
                case GameInputForceFeedbackEffectKind.GameInputForceFeedbackSpring:
                    result.Spring = condition;
                    break;
                case GameInputForceFeedbackEffectKind.GameInputForceFeedbackFriction:
                    result.Friction = condition;
                    break;
                case GameInputForceFeedbackEffectKind.GameInputForceFeedbackDamper:
                    result.Damper = condition;
                    break;
                case GameInputForceFeedbackEffectKind.GameInputForceFeedbackInertia:
                    result.Inertia = condition;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(kind), kind, "必須指定 condition force feedback 類型。");
            }

            return result;
        }
    }
}
