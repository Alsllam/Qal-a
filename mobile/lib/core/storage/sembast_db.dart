import 'package:path/path.dart' as p;
import 'package:path_provider/path_provider.dart';
import 'package:sembast/sembast_io.dart';

/// Opens the app's structured local database.
abstract final class SembastDb {
  static Future<Database> open() async {
    final dir = await getApplicationDocumentsDirectory();
    return await databaseFactoryIo.openDatabase(p.join(dir.path, 'qala.db'));
  }
}
