from flask import Blueprint, request, jsonify
from models import User, Instance
from extensions import db

shop_bp = Blueprint("shop", __name__, url_prefix="/shop")

# 경매장에 올라온 전체 목록
# sellState 활성화 된거 모조리 Select 해서 보내주면 될듯 
# - (OID, Cost, bigClass, smallClass, stat) 반환
# SELECT OID, Cost, bigClass, smallClass, stat FROM _TABLE_ WHERE sellState = 1
# 이걸 기반으로 경매장 모든 매물 리스트 만들어서 표시
@shop_bp.route("/onsale", methods=["GET"])
def onSale():
    items = Instance.query.filter_by(sellState=1).all()
    # 필요한 필드만 추출
    try:
        data = [
            {
                "uid": item.uid,
                "oid": item.oid,
                "cost": item.cost,
                "bigClass": item.bigClass,
                "smallClass": item.smallClass,
                "stat": item.stat
            }
            for item in items
        ]

        return jsonify({"success": True, "data": data}), 200

    except Exception as e:
        db.session.rollback()
        return jsonify({"success": False, "error": str(e)}), 500

# 내가 올린 전체 목록
# sellState && uid 를 SELECT 해서 보내주면 될듯 
# - (OID, Cost, bigClass, smallClass, stat) 반환
# SELECT OID, Cost, bigClass, smallClass, stat FROM _TABLE_ WHERE sellState = 1 AND uid = myUid
# 이걸 기반으로 내가 올린 경매장 모든 매물 리스트 만들어서 표시, 유저는 올린거 취소할 수도 있음
@shop_bp.route("/<int:uid>", methods=["GET"])
def myObject(uid):
    try:
        # 해당 uid가 올린 sellState=1인 모든 매물 조회
        items = Instance.query.filter_by(uid=uid, sellState=1).all()

        # 필요한 필드만 추출하여 리스트 구성
        result = [
            {
                "oid": item.oid,
                "cost": item.cost,
                "bigClass": item.bigClass,
                "smallClass": item.smallClass,
                "stat": item.stat
            }
            for item in items
        ]

        return jsonify({"success": True, "data": result}), 200

    except Exception as e:
        db.session.rollback()
        return jsonify({"success": False, "error": str(e)}), 500
    

@shop_bp.route("/buy", methods=["POST"])
def buy_instance():
    data = request.get_json()
    uid = int(data['uid'])  # 구매자 uid
    oid = int(data['oid'])  # 구매할 아이템 oid

    if not uid or not oid:
        return jsonify({"success": "false", "message": "Invalid request"}), 400
    
    try:
        # 구매자와 아이템 데이터 조회
        buyer = User.query.filter_by(uid=uid).first()
        item = Instance.query.filter_by(oid=oid).first()

        if not buyer or not item:
            return jsonify({"success": "false", "message": "User or Item not found"}), 404

        # 이미 판매중인지 확인
        if item.sellState != True:
            return jsonify({"success": "false", "message": "Item not on sale"}), 400

        # 구매자 돈 확인
        if buyer.money < item.cost:
            return jsonify({"success": "false", "message": "Not enough money"}), 400

        # 트랜잭션: 구매 처리
        seller = User.query.filter_by(uid=item.uid).first() if item.uid != 0 else None

        buyer.money -= item.cost
        if seller:
            seller.money += item.cost

        # 소유권 이전
        item.uid = buyer.uid
        item.cost = -1
        item.sellState = False
        print(item.uid, item.cost, item.sellState)

        db.session.commit()

        return jsonify({"success": "true", "message": buyer.money}), 200

    except Exception as e:
        db.session.rollback()
        print("[구매 오류]", e)
        return jsonify({"success": "false", "message": "Server error"}), 500