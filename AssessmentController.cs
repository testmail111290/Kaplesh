Please copy Resource Group Credentials from the Screen.

1. Create Virtual Private Network by command
az network vnet create --subnet-name Private  --location "East US" --resource-group user-lvodyfwaeoim --name pooja

2. Create NSG Command
az network nsg create  --resource-group user-uqccsuhwveds --name pooja 

az vm create \
    --resource-group  user-lctfmfkrublp \
    --name poojaVM \
    --image win2016datacenter \
    --admin-username azureuser \
    --admin-password Azurepassword123# \
    --generate-ssh-keys 
