from flask import Blueprint, request, jsonify
from models import Cache, Instance
from extensions import db

cache_bp = Blueprint("cache", __name__, url_prefix="/cache")

# 캐시 생성
@cache_bp.route("/create", methods=["POST"])    
def create_cache():
    data = request.get_json()
    new_cache = Cache(
        uid=data["uid"],
        oid=data["oid"]
    )

    # 데이터베이스에 캐시 추가
    db.session.add(new_cache)
    db.session.commit()
    return jsonify({"message": "Cache created"}), 201

# 캐시 조회
@cache_bp.route("/<int:oid>", methods=["GET"])
def get_cache(oid):
    cache = Cache.query.get(oid)
    if not cache:
        return jsonify({"message": "Cache not found"}), 404

    cache_data = Instance.query.get(oid)
    return jsonify(cache_data), 200