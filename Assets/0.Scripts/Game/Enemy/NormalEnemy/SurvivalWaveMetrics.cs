using System;
using System.Globalization;

[Serializable]
public struct SurvivalWaveMetrics
{
    public float elapsedSeconds;
    public int waveIndex, targetAttempts, attempted, successful, capSkipped, poolFailed, frameSkipped;
    public int activeNormal, peakActive, normalKills, playerLevel;
    public float xpDropped, rawCollected, xpAwarded;
    public bool catchupActive;
    public const string CsvHeader = "elapsedSec,waveIndex,spawnTarget,attempted,spawnSuccess,capSkipped,poolFailed,frameSkipped,activeNormal,peakActiveNormal,normalKills,xpDropped,rawCollected,xpAwarded,playerLevel,catchupActive";
    public string ToCsv() => string.Join(",", elapsedSeconds.ToString("0.###", CultureInfo.InvariantCulture), waveIndex,
        targetAttempts, attempted, successful, capSkipped, poolFailed, frameSkipped, activeNormal, peakActive, normalKills,
        xpDropped.ToString(CultureInfo.InvariantCulture), rawCollected.ToString(CultureInfo.InvariantCulture),
        xpAwarded.ToString(CultureInfo.InvariantCulture), playerLevel, catchupActive ? 1 : 0);
}
