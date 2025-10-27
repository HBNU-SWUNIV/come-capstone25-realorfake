from extensions import db
from sqlalchemy.orm import validates

class Instance(db.Model):
    __tablename__ = 'instance'

    oid = db.Column(db.Integer, primary_key=True)
    uid = db.Column(db.Integer, db.ForeignKey('user.uid', ondelete='CASCADE'), nullable=False)
    bigClass = db.Column(db.String(20), nullable=False)
    smallClass = db.Column(db.String(20), nullable=False)
    abilityType = db.Column(db.String(20), nullable=False)
    sellState = db.Column(db.Boolean, nullable=False)
    cost = db.Column(db.Integer, nullable=True)
    expireCount = db.Column(db.SmallInteger, nullable=True)
    stat = db.Column(db.Integer, nullable=False)
    grade = db.Column(db.String(20), nullable=False)

    @classmethod
    def create(cls, data):
        instance = cls(**data)
        db.session.add(instance)
        db.session.commit()
        return instance

    @classmethod
    def get_by_id(cls, oid):
        return cls.query.get(oid)

    @classmethod
    def get_all(cls):
        return cls.query.all()

    @classmethod
    def update(cls, oid, data):
        instance = cls.query.get(oid)
        if not instance:
            return None
        for key, value in data.items():
            setattr(instance, key, value)
        db.session.commit()
        return instance

    @classmethod
    def delete(cls, oid):
        instance = cls.query.get(oid)
        if not instance:
            return None
        db.session.delete(instance)
        db.session.commit()
        return instance
    
    @validates("sellState")
    def validate_sellState(self, key, value):
        if isinstance(value, str):
            return value.lower() in ("true", "1", "y", "yes", "on")
        return bool(value)