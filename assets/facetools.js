postParamBuildSteps.push(() => {
    let reactorGroup = document.getElementById('input_group_content_reactor');
    if (reactorGroup) {
        if (!currentBackendFeatureSet.includes('ipadapter')) {
            reactorGroup.append(createDiv(`reactor_ipadapter_install_button`, 'keep_group_visible', `<button class="basic-button" onclick="installFeatureById('ipadapter', 'reactor_ipadapter_install_button')">Install IP Adapter</button>`));
        }
        // IP Adapter needs to be installed first so only show this button if that's ready
        if (currentBackendFeatureSet.includes('ipadapter') && !currentBackendFeatureSet.includes('reactor')) {
            reactorGroup.append(createDiv(`reactor_install_button`, 'keep_group_visible', `<button class="basic-button" onclick="installFeatureById('reactor', 'reactor_install_button')">Install ReActor</button>`));
        }
    }
    
    // Disabled until PR is merged to fix this node working in differently named custom_node paths
//    let facerestorecfGroup = document.getElementById('input_group_content_facerestorecf');
//    if (facerestorecfGroup) {
//        if (!currentBackendFeatureSet.includes('ipadapter')) {
//            facerestorecfGroup.append(createDiv(`facerestorecf_ipadapter_install_button`, 'keep_group_visible', `<button class="basic-button" onclick="installFeatureById('ipadapter', 'facerestorecf_ipadapter_install_button')">Install IP Adapter</button>`));
//        }
//        // IP Adapter needs to be installed first so only show this button if that's ready
//        if (currentBackendFeatureSet.includes('ipadapter') && !currentBackendFeatureSet.includes('facerestorecf')) {
//            facerestorecfGroup.append(createDiv(`facerestorecf_install_button`, 'keep_group_visible', `<button class="basic-button" onclick="installFeatureById('facerestorecf', 'facerestorecf_install_button')">Install FaceRestoreCF</button>`));
//        }
//    }
});
