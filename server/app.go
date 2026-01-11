package main

import (
	"bytes"
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
	CreatedAt time.Time `gorm:"autoCreateTime" json:"created_at"`
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

func (n *Note) ReturnHTML() (string, error) {
	if n == nil || n.Naiyou == "" {
		return "", nil
	}
	var buf bytes.Buffer
	if err := goldmark.Convert([]byte(n.Naiyou), &buf); err != nil {
		return "", err
	}
	return buf.String(), nil
}

func (n *Note) update_time() {
	n.UpdatedAt = time.Now()
}

func load_note(user User, keyword string) []Note {
	var notes []Note
	db.Where("user_id = ? AND name LIKE ?", user.ID, "%"+keyword+"%").Find(&notes)
	return notes
}
