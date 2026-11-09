FROM node:22-alpine
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci --omit=dev
COPY server.js ./
ENV PORT=5080 PUBLIC_URL=http://localhost:5080
EXPOSE 5080
CMD ["node", "server.js"]
