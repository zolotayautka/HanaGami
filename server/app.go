package main

import (
	"bytes"
	"encoding/json"
	"time"

	"github.com/yuin/goldmark"
	"gorm.io/gorm"
)

var db *gorm.DB

type Note struct {
	GlobalID  int       `gorm:"primaryKey;autoIncrement" json:"global_id"`
	Name      string    `gorm:"not null" json:"name"`
	Naiyou    string    `gorm:"type:text" json:"naiyou,omitempty"`
	Revison   int       `gorm:"not null;default:0" json:"revision"`
	UpdatedAt time.Time `gorm:"autoUpdateTime" json:"updated_at"`
	UserID    int       `gorm:"not null;index" json:"user_id"`
	User      User      `gorm:"foreignKey:UserID;references:ID;constraint:OnDelete:CASCADE" json:"user"`
}

type User struct {
	ID       int    `gorm:"primaryKey;autoIncrement" json:"id"`
	Username string `gorm:"unique;not null" json:"username"`
	Email    string `gorm:"unique;not null" json:"email"`
	Password string `gorm:"not null" json:"password"`
	Notes    []Note `gorm:"foreignKey:UserID" json:"notes,omitempty"`
}

func (n *Note) ReturnHTML() string {
	if n == nil || n.Naiyou == "" {
		return ""
	}
	var buf bytes.Buffer
	if err := goldmark.Convert([]byte(n.Naiyou), &buf); err != nil {
		return ""
	}
	return buf.String()
}

func (n *Note) update_time() {
	n.UpdatedAt = time.Now()
}

func load_note(user User, keyword string) string {
	var notes []Note
	db.Omit("Naiyou").Where("user_id = ? AND name LIKE ?", user.ID, "%"+keyword+"%").Find(&notes)
	notes_json, _ := json.Marshal(notes)
	return string(notes_json)
}

func get_Note(global_id int) string {
	var note Note
	db.Preload("User").First(&note, global_id)
	return note.ReturnHTML()
}

func add_note(new_note Note) bool {
	new_note.update_time()
	result := db.Create(&new_note)
	return result.Error == nil
}

func update_note(updated_note Note) bool {
	updated_note.update_time()
	result := db.Model(&Note{}).Where("global_id = ?", updated_note.GlobalID).Updates(updated_note)
	return result.Error == nil
}

func delete_note(note_to_delete Note) bool {
	result := db.Delete(&Note{}, note_to_delete.GlobalID)
	return result.Error == nil
}
