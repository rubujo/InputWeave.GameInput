using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// The managed helper for constructing GameInput force feedback and rumble parameters.
/// 建立 GameInput force feedback 與 rumble 參數的 managed helper。
/// </summary>
public static class GameInputForceFeedback
{
    /// <summary>
    /// Creates rumble parameters.
    /// 建立 rumble 參數。
    /// </summary>
    /// <param name="lowFrequency">The low-frequency motor intensity. 低頻馬達強度。</param>
    /// <param name="highFrequency">The high-frequency motor intensity. 高頻馬達強度。</param>
    /// <param name="leftTrigger">The left trigger motor intensity. 左 trigger 馬達強度。</param>
    /// <param name="rightTrigger">The right trigger motor intensity. 右 trigger 馬達強度。</param>
    /// <returns>The constructed rumble parameters. 建構完成的 rumble 參數。</returns>
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

    /// <summary>
    /// Creates force feedback envelope parameters.
    /// 建立 force feedback envelope 參數。
    /// </summary>
    /// <param name="attackDuration">The attack phase duration. Attack 階段持續時間。</param>
    /// <param name="sustainDuration">The sustain phase duration. Sustain 階段持續時間。</param>
    /// <param name="releaseDuration">The release phase duration. Release 階段持續時間。</param>
    /// <param name="attackGain">The attack phase gain. Attack 階段 gain。</param>
    /// <param name="sustainGain">The sustain phase gain. Sustain 階段 gain。</param>
    /// <param name="releaseGain">The release phase gain. Release 階段 gain。</param>
    /// <param name="playCount">The number of times to play. 播放次數。</param>
    /// <param name="repeatDelay">The delay before each repeat. 重複播放前的延遲。</param>
    /// <returns>The constructed force feedback envelope. 建構完成的 force feedback envelope。</returns>
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

    /// <summary>
    /// Creates force feedback magnitude parameters.
    /// 建立 force feedback magnitude 參數。
    /// </summary>
    /// <param name="linearX">The linear magnitude on the X axis. X 軸線性強度。</param>
    /// <param name="linearY">The linear magnitude on the Y axis. Y 軸線性強度。</param>
    /// <param name="linearZ">The linear magnitude on the Z axis. Z 軸線性強度。</param>
    /// <param name="angularX">The angular magnitude on the X axis. X 軸角向強度。</param>
    /// <param name="angularY">The angular magnitude on the Y axis. Y 軸角向強度。</param>
    /// <param name="angularZ">The angular magnitude on the Z axis. Z 軸角向強度。</param>
    /// <param name="normal">The normalized magnitude. 一般化強度。</param>
    /// <returns>The constructed force feedback magnitude. 建構完成的 force feedback magnitude。</returns>
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

    /// <summary>
    /// Creates constant force feedback effect parameters.
    /// 建立 constant force feedback effect 參數。
    /// </summary>
    /// <param name="magnitude">The effect magnitude. Effect 強度。</param>
    /// <param name="envelope">Effect envelope。</param>
    /// <returns>The constructed force feedback effect parameters. 建構完成的 force feedback effect 參數。</returns>
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

    /// <summary>
    /// Creates ramp force feedback effect parameters.
    /// 建立 ramp force feedback effect 參數。
    /// </summary>
    /// <param name="startMagnitude">The starting magnitude. 起始強度。</param>
    /// <param name="endMagnitude">The ending magnitude. 結束強度。</param>
    /// <param name="envelope">Effect envelope。</param>
    /// <returns>The constructed force feedback effect parameters. 建構完成的 force feedback effect 參數。</returns>
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

    /// <summary>
    /// Creates sine wave force feedback effect parameters.
    /// 建立 sine wave force feedback effect 參數。
    /// </summary>
    /// <param name="magnitude">The effect magnitude. Effect 強度。</param>
    /// <param name="envelope">Effect envelope。</param>
    /// <param name="frequency">The periodic frequency. 週期頻率。</param>
    /// <param name="phase">The periodic phase. 週期相位。</param>
    /// <param name="bias">The periodic bias. 週期偏移。</param>
    /// <returns>The constructed force feedback effect parameters. 建構完成的 force feedback effect 參數。</returns>
    public static GameInputForceFeedbackParams SineWave(GameInputForceFeedbackMagnitude magnitude, GameInputForceFeedbackEnvelope envelope, float frequency, float phase = 0, float bias = 0)
    {
        return Periodic(GameInputForceFeedbackEffectKind.GameInputForceFeedbackSineWave, magnitude, envelope, frequency, phase, bias);
    }

    /// <summary>
    /// Creates square wave force feedback effect parameters.
    /// 建立 square wave force feedback effect 參數。
    /// </summary>
    /// <param name="magnitude">The effect magnitude. Effect 強度。</param>
    /// <param name="envelope">Effect envelope。</param>
    /// <param name="frequency">The periodic frequency. 週期頻率。</param>
    /// <param name="phase">The periodic phase. 週期相位。</param>
    /// <param name="bias">The periodic bias. 週期偏移。</param>
    /// <returns>The constructed force feedback effect parameters. 建構完成的 force feedback effect 參數。</returns>
    public static GameInputForceFeedbackParams SquareWave(GameInputForceFeedbackMagnitude magnitude, GameInputForceFeedbackEnvelope envelope, float frequency, float phase = 0, float bias = 0)
    {
        return Periodic(GameInputForceFeedbackEffectKind.GameInputForceFeedbackSquareWave, magnitude, envelope, frequency, phase, bias);
    }

    /// <summary>
    /// Creates triangle wave force feedback effect parameters.
    /// 建立 triangle wave force feedback effect 參數。
    /// </summary>
    /// <param name="magnitude">The effect magnitude. Effect 強度。</param>
    /// <param name="envelope">Effect envelope。</param>
    /// <param name="frequency">The periodic frequency. 週期頻率。</param>
    /// <param name="phase">The periodic phase. 週期相位。</param>
    /// <param name="bias">The periodic bias. 週期偏移。</param>
    /// <returns>The constructed force feedback effect parameters. 建構完成的 force feedback effect 參數。</returns>
    public static GameInputForceFeedbackParams TriangleWave(GameInputForceFeedbackMagnitude magnitude, GameInputForceFeedbackEnvelope envelope, float frequency, float phase = 0, float bias = 0)
    {
        return Periodic(GameInputForceFeedbackEffectKind.GameInputForceFeedbackTriangleWave, magnitude, envelope, frequency, phase, bias);
    }

    /// <summary>
    /// Creates sawtooth up wave force feedback effect parameters.
    /// 建立 sawtooth up wave force feedback effect 參數。
    /// </summary>
    /// <param name="magnitude">The effect magnitude. Effect 強度。</param>
    /// <param name="envelope">Effect envelope。</param>
    /// <param name="frequency">The periodic frequency. 週期頻率。</param>
    /// <param name="phase">The periodic phase. 週期相位。</param>
    /// <param name="bias">The periodic bias. 週期偏移。</param>
    /// <returns>The constructed force feedback effect parameters. 建構完成的 force feedback effect 參數。</returns>
    public static GameInputForceFeedbackParams SawtoothUpWave(GameInputForceFeedbackMagnitude magnitude, GameInputForceFeedbackEnvelope envelope, float frequency, float phase = 0, float bias = 0)
    {
        return Periodic(GameInputForceFeedbackEffectKind.GameInputForceFeedbackSawtoothUpWave, magnitude, envelope, frequency, phase, bias);
    }

    /// <summary>
    /// Creates sawtooth down wave force feedback effect parameters.
    /// 建立 sawtooth down wave force feedback effect 參數。
    /// </summary>
    /// <param name="magnitude">The effect magnitude. Effect 強度。</param>
    /// <param name="envelope">Effect envelope。</param>
    /// <param name="frequency">The periodic frequency. 週期頻率。</param>
    /// <param name="phase">The periodic phase. 週期相位。</param>
    /// <param name="bias">The periodic bias. 週期偏移。</param>
    /// <returns>The constructed force feedback effect parameters. 建構完成的 force feedback effect 參數。</returns>
    public static GameInputForceFeedbackParams SawtoothDownWave(GameInputForceFeedbackMagnitude magnitude, GameInputForceFeedbackEnvelope envelope, float frequency, float phase = 0, float bias = 0)
    {
        return Periodic(GameInputForceFeedbackEffectKind.GameInputForceFeedbackSawtoothDownWave, magnitude, envelope, frequency, phase, bias);
    }

    /// <summary>
    /// Creates periodic force feedback effect parameters.
    /// 建立 periodic force feedback effect 參數。
    /// </summary>
    /// <param name="kind">The effect kind. Effect 類型。</param>
    /// <param name="magnitude">The effect magnitude. Effect 強度。</param>
    /// <param name="envelope">Effect envelope。</param>
    /// <param name="frequency">The periodic frequency. 週期頻率。</param>
    /// <param name="phase">The periodic phase. 週期相位。</param>
    /// <param name="bias">The periodic bias. 週期偏移。</param>
    /// <returns>The constructed force feedback effect parameters. 建構完成的 force feedback effect 參數。</returns>
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

    /// <summary>
    /// Creates spring force feedback effect parameters.
    /// 建立 spring force feedback effect 參數。
    /// </summary>
    /// <param name="condition">The condition effect parameters. Condition effect 參數。</param>
    /// <returns>The constructed force feedback effect parameters. 建構完成的 force feedback effect 參數。</returns>
    public static GameInputForceFeedbackParams Spring(GameInputForceFeedbackConditionParams condition)
    {
        return Condition(GameInputForceFeedbackEffectKind.GameInputForceFeedbackSpring, condition);
    }

    /// <summary>
    /// Creates friction force feedback effect parameters.
    /// 建立 friction force feedback effect 參數。
    /// </summary>
    /// <param name="condition">The condition effect parameters. Condition effect 參數。</param>
    /// <returns>The constructed force feedback effect parameters. 建構完成的 force feedback effect 參數。</returns>
    public static GameInputForceFeedbackParams Friction(GameInputForceFeedbackConditionParams condition)
    {
        return Condition(GameInputForceFeedbackEffectKind.GameInputForceFeedbackFriction, condition);
    }

    /// <summary>
    /// Creates damper force feedback effect parameters.
    /// 建立 damper force feedback effect 參數。
    /// </summary>
    /// <param name="condition">The condition effect parameters. Condition effect 參數。</param>
    /// <returns>The constructed force feedback effect parameters. 建構完成的 force feedback effect 參數。</returns>
    public static GameInputForceFeedbackParams Damper(GameInputForceFeedbackConditionParams condition)
    {
        return Condition(GameInputForceFeedbackEffectKind.GameInputForceFeedbackDamper, condition);
    }

    /// <summary>
    /// Creates inertia force feedback effect parameters.
    /// 建立 inertia force feedback effect 參數。
    /// </summary>
    /// <param name="condition">The condition effect parameters. Condition effect 參數。</param>
    /// <returns>The constructed force feedback effect parameters. 建構完成的 force feedback effect 參數。</returns>
    public static GameInputForceFeedbackParams Inertia(GameInputForceFeedbackConditionParams condition)
    {
        return Condition(GameInputForceFeedbackEffectKind.GameInputForceFeedbackInertia, condition);
    }

    /// <summary>
    /// Creates condition force feedback effect parameters.
    /// 建立 condition force feedback effect 參數。
    /// </summary>
    /// <param name="kind">The effect kind. Effect 類型。</param>
    /// <param name="condition">The condition effect parameters. Condition effect 參數。</param>
    /// <returns>The constructed force feedback effect parameters. 建構完成的 force feedback effect 參數。</returns>
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
