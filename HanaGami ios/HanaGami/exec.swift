import Foundation
import Ink

struct Node: Codable, Hashable {
    let local_id: Int
    let global_id: Int
    var name: String
    var naiyou: String?
    
    var revison: Int
    var is_shared: Bool
    var dirty: Bool
    var is_deleted: Bool
    
    let created_at: Date
    var updated_at: Date
    
    func return_html() -> String {
        guard let md = naiyou, !md.isEmpty else { return "" }
        let parser = MarkdownParser()
        return parser.html(from: md)
    }

    init(local_id: Int,
         global_id: Int,
         name: String,
         naiyou: String? = nil,
         revison: Int = 0,
         is_shared: Bool = false,
         dirty: Bool = false,
         is_deleted: Bool = false,
         created_at: Date = Date(),
         updated_at: Date = Date()) {
        self.local_id = local_id
        self.global_id = global_id
        self.name = name
        self.naiyou = naiyou
        self.revison = revison
        self.is_shared = is_shared
        self.dirty = dirty
        self.is_deleted = is_deleted
        self.created_at = created_at
        self.updated_at = updated_at
    }
}

func load_list() -> [Note] {
    sync()
    return load_local()
}

func sync() {
    //
}

func load_local(keyword: String = "") -> [Note] {
    let cPath = dbPath!.cString(using: .utf8);
    var db: OpaquePointer? = nil
    var stmt: OpaquePointer? = nil
    var sql = "SELECT local_id, global_id, name, naiyou, revison, is_shared, dirty, is_deleted, created_at, updated_at FROM notes WHERE is_deleted = 0"
    if !keyword.isEmpty {
        sql += " AND (name LIKE '%\(keyword)%' OR naiyou LIKE '%\(keyword)%')"
    }
    sql += " ORDER BY updated_at DESC;"
    var notes: [Note] = []
    if sqlite3_open(cPath, &db) == SQLITE_OK {
        if sqlite3_prepare_v2(db, sql, -1, &stmt, nil) == SQLITE_OK {
            while sqlite3_step(stmt) == SQLITE_ROW {
                let local_id = Int(sqlite3_column_int(stmt, 0))
                let global_id = Int(sqlite3_column_int(stmt, 1))
                let name = String(cString: sqlite3_column_text(stmt, 2))
                let naiyouPtr = sqlite3_column_text(stmt, 3)
                let naiyou = naiyouPtr != nil ? String(cString: naiyouPtr!) : nil
                let revison = Int(sqlite3_column_int(stmt, 4))
                let is_shared = sqlite3_column_int(stmt, 5) != 0
                let dirty = sqlite3_column_int(stmt, 6) != 0
                let is_deleted = sqlite3_column_int(stmt, 7) != 0
                let created_at = Date(timeIntervalSince1970: TimeInterval(sqlite3_column_double(stmt, 8)))
                let updated_at = Date(timeIntervalSince1970: TimeInterval(sqlite3_column_double(stmt, 9)))
                let note = Note(local_id: local_id,
                                global_id: global_id,
                                name: name,
                                naiyou: naiyou,
                                revison: revison,
                                is_shared: is_shared,
                                dirty: dirty,
                                is_deleted: is_deleted,
                                created_at: created_at,
                                updated_at: updated_at)
                notes.append(note)
            }
        }
        sqlite3_finalize(stmt)
    }
    sqlite3_close(db)
    return notes
}

func add_note(name: String, naiyou: String?, is_shared: Bool) {
    let cPath = dbPath!.cString(using: .utf8);
    var db: OpaquePointer? = nil
    var stmt: OpaquePointer? = nil
    let sql = "INSERT INTO notes (global_id, name, naiyou, revison, is_shared, dirty, is_deleted, created_at, updated_at) VALUES (0, ?, ?, 0, ?, 1, 0, ?, ?);"
    let now = Date().timeIntervalSince1970
    if sqlite3_open(cPath, &db) == SQLITE_OK {
        if sqlite3_prepare_v2(db, sql, -1, &stmt, nil) == SQLITE_OK {
            sqlite3_bind_text(stmt, 1, (name as NSString).utf8String, -1, nil)
            if let naiyou = naiyou {
                sqlite3_bind_text(stmt, 2, (naiyou as NSString).utf8String, -1, nil)
            } else {
                sqlite3_bind_null(stmt, 2)
            }
            sqlite3_bind_int(stmt, 3, is_shared ? 1 : 0)
            sqlite3_bind_double(stmt, 4, now)
            sqlite3_bind_double(stmt, 5, now)
            if sqlite3_step(stmt) != SQLITE_DONE {}
        }
        sqlite3_finalize(stmt)
    }
    sqlite3_close(db)
    if is_shared {
        sync()
    }
}