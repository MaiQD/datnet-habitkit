# Deployment Guide - HabitKit Clone

This guide covers deploying the HabitKit Clone application to Fly.io using GitHub Actions CI/CD.

## Prerequisites

1. **Fly.io Account**: Sign up at [fly.io](https://fly.io)
2. **Fly.io CLI**: Install the Fly.io CLI tool
3. **GitHub Repository**: Your code should be in a GitHub repository

## Initial Setup

### 1. Install Fly.io CLI

```bash
# macOS
brew install flyctl

# Windows (PowerShell)
iwr https://fly.io/install.ps1 -useb | iex

# Linux
curl -L https://fly.io/install.sh | sh
```

### 2. Login to Fly.io

```bash
flyctl auth login
```

### 3. Launch the App (First Time Only)

From the project root directory:

```bash
# This will create the app and fly.toml configuration
flyctl launch --no-deploy

# Create a persistent volume for the database
flyctl volumes create habitkit_data --region sjc --size 1

# Set up secrets (Google OAuth credentials)
flyctl secrets set Authentication__Google__ClientId="your-google-client-id"
flyctl secrets set Authentication__Google__ClientSecret="your-google-client-secret"
```

### 4. Configure GitHub Secrets

In your GitHub repository, go to Settings → Secrets and variables → Actions, and add:

- `FLY_API_TOKEN`: Get this from [Fly.io dashboard](https://fly.io/dashboard/personal/tokens)

## Deployment Process

### Automatic Deployment

The app automatically deploys when you push to the `main` branch:

1. **Push to main**: Triggers GitHub Actions workflow
2. **Build & Test**: Runs on every push and PR
3. **Deploy**: Only runs on main branch pushes
4. **Health Check**: Verifies deployment success

### Manual Deployment

```bash
# Deploy manually
flyctl deploy

# Deploy with logs
flyctl deploy --verbose
```

## Monitoring and Management

### View Logs

```bash
# Real-time logs
flyctl logs

# Logs with timestamps
flyctl logs --timestamps

# Follow logs
flyctl logs -f
```

### Check App Status

```bash
# App status
flyctl status

# App info
flyctl info

# List machines
flyctl machine list
```

### Scale the App

```bash
# Scale to 1 machine (free tier)
flyctl scale count 1

# Scale to 2 machines (paid)
flyctl scale count 2
```

## Database Management

### Backup Database

```bash
# SSH into the machine
flyctl ssh console

# Inside the machine, copy database
cp /data/app.db /tmp/backup.db

# Download backup
flyctl ssh sftp get /tmp/backup.db ./backup-$(date +%Y%m%d).db
```

### Restore Database

```bash
# Upload database file
flyctl ssh sftp put ./backup.db /tmp/restore.db

# SSH and restore
flyctl ssh console
cp /tmp/restore.db /data/app.db
```

## Troubleshooting

### Common Issues

1. **App won't start**: Check logs with `flyctl logs`
2. **Database issues**: Verify volume is mounted at `/data`
3. **Build failures**: Check GitHub Actions logs
4. **Health check failures**: Verify app responds on port 8080

### Debug Commands

```bash
# SSH into running machine
flyctl ssh console

# Check disk usage
flyctl ssh console -C "df -h"

# Check running processes
flyctl ssh console -C "ps aux"

# Check environment variables
flyctl ssh console -C "env | grep ASPNETCORE"
```

## Cost Management

### Free Tier Limits

- **3 shared-cpu VMs** with 256MB RAM each
- **1GB persistent volume** (included)
- **160GB-hours** of VM time per month
- Apps auto-stop when idle (20+ minutes)

### Monitoring Usage

```bash
# Check current usage
flyctl dashboard

# View billing
flyctl billing show
```

### Cost Optimization

1. **Single VM**: Keep `min_machines_running = 0` in fly.toml
2. **Auto-stop**: Enable `auto_stop_machines = true`
3. **Monitor**: Check usage regularly in dashboard

## Security

### Environment Variables

Never commit secrets to code. Use Fly.io secrets:

```bash
# Set secrets
flyctl secrets set KEY_NAME="value"

# List secrets
flyctl secrets list

# Remove secret
flyctl secrets unset KEY_NAME
```

### HTTPS

HTTPS is automatically handled by Fly.io proxy. No additional configuration needed.

## Maintenance

### Updates

1. Push changes to main branch
2. GitHub Actions automatically builds and deploys
3. Monitor deployment in GitHub Actions tab

### Database Migrations

Migrations are automatically applied on startup. No manual intervention needed.

### Rollbacks

```bash
# Deploy previous version
flyctl deploy --image flyio/habitkit-clone:previous

# Or use GitHub Actions to re-run previous deployment
```

## Support

- **Fly.io Docs**: [fly.io/docs](https://fly.io/docs)
- **GitHub Actions**: Check Actions tab in your repository
- **App Logs**: Use `flyctl logs` for debugging
