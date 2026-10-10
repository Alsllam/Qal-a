import 'package:flutter_bloc/flutter_bloc.dart';

import 'package:qala/features/ladder/domain/entities/ladder_entry.dart';
import 'package:qala/features/ladder/domain/usecases/ladder_usecases.dart';

class LadderCubit extends Cubit<List<LadderEntry>> {
  new(this._getLadder) : super(_getLadder());

  final GetLadder _getLadder;

  void refresh() => emit(_getLadder());
}
