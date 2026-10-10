abstract interface class LadderRepository {
  /// Highest level beaten so far (0 = none).
  int highestBeaten();
  Future<void> saveHighestBeaten(int level);
}
