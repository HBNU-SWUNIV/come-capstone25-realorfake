FROM nvcr.io/nvidia/pytorch:24.03-py3

WORKDIR /app

COPY requirements.txt .

RUN pip install --no-cache-dir -r requirements.txt \
    && pip install --no-cache-dir "numpy<2.0" "scipy<1.11"

COPY . .

EXPOSE 3333

ENV NVIDIA_VISIBLE_DEVICES=all
ENV NVIDIA_DRIVER_CAPABILITIES=compute,utility

CMD ["python", "app.py"]