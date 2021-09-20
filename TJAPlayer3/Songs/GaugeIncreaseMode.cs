namespace TJAPlayer3
{
    /// <summary>
    /// ゲージ増加量の種類。
    /// </summary>
    public enum GaugeIncreaseMode
    {
        /// <summary>
        /// 切り捨てる。Floorと同義。
        /// </summary>
        Normal,
        /// <summary>
        /// 切り捨てる。
        /// </summary>
        Floor,
        /// <summary>
        /// 四捨五入する。
        /// </summary>
        Round,
        /// <summary>
        /// 切り上げる。
        /// </summary>
        Ceiling,
        /// <summary>
        /// 丸め処理を行わない。
        /// </summary>
        NotFix
    }
}
